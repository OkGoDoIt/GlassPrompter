using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Media;
using Android.Speech;
using System.Collections.Generic;
using Android.Util;
using Android.Content.PM;
using Android.Graphics;
using Android.Text;


using System.Linq;
using Java.Util;
using System.Net;
using Android.Glass.Touchpad;



namespace GlassPrompter
{
	[Activity(Label = "GlassPrompter", MainLauncher = true, Icon = "@drawable/icon", Enabled = true)]
	[IntentFilter(new String[] { "com.google.android.glass.action.VOICE_TRIGGER" })]
	[MetaData("com.google.android.glass.VoiceTrigger", Resource = "@xml/my_voice_trigger")]
	public class Activity1 : Activity
	{
		public AudioManager audioManager;
		public SpeechRecognizer speechRecognizer;
		public Intent speechRecognizerIntent;
		public Messenger mServerMessenger;
		ComponentName speechComp;

		private Android.Glass.Touchpad.GestureDetector mGestureDetector;

		public bool mIsListening;
		public bool mIsCountDownOn;

		const int MSG_RECOGNIZER_START_LISTENING = 1;
		const int MSG_RECOGNIZER_CANCEL = 2;

		const int lineMaxLength = 30;

		bool isRecoing = false;

		Timer timer = new Timer();

		List<TextView> displayLines;
		GridView indicator;
		LinearLayout layoutRoot;
		ScrollView scroller;
		LinearLayout listLayout;

		string fullScript = @"This is Glass Prompter, a teleprompter for Google Glass. // It tells me what to say so I don't mess up my speech, while keeping me focused on all of you. By the way, this cable is just so you can see it onscreen, it's not normally required. //
Glass Prompter is actively listening to what I'm saying, using the AT&T speech recognition API, allowing it to adjust its speed to match my pace.  If I slow down... it is still right where I need it to be. // And since it's following along with me, it auto advances my slides as I speak. //
Now 90 seconds is not a long time, so Glass Prompter will nudge me to speed up if I fall behind, keeping me on the right pace. //
Wearable technology, sensors, and big data can do amazing things, but sometimes the most useful innovations are right in front of your eyes.
My name is Roger Pincombe, and I built Glass Prompter to scratch my own itch as a professional hackathonner. I think many of the brilliant presenters taking the stage today will love it as well.";



		int curLine = 0;
		int lineCount = 0;




		int scrollTarget = 0;
		int scrollDelta = 0;

		Handler scrollTmr = new Handler();

		List<string> scriptLines;
		List<int> slideMapping = new List<int>();


		private Android.Glass.Touchpad.GestureDetector createGestureDetector(Context context)
		{
			Android.Glass.Touchpad.GestureDetector gestureDetector = new Android.Glass.Touchpad.GestureDetector(context);
			gestureDetector.SetBaseListener(new GestureDetectorImpl(this));
			return gestureDetector;

		}


		List<string> getLinesFromScript(string inputScript)
		{
			List<string> lines = new List<string>();
			inputScript = inputScript.Replace('`', '\'').Replace(System.Environment.NewLine, " ` ");
			string[] words = inputScript.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			string currentLine = "";
			int curSlide = 0;

			for (int i = 0; i < words.Length; i++)
			{
				if (words[i] == "`")
				{
					if (currentLine.Trim().EndsWith("//"))
					{
						slideMapping.Add(curSlide);
					}
					else
					{
						slideMapping.Add(curSlide + 1);
					}

					if (currentLine.Contains("//"))
					{
						curSlide++;
					}



					lines.Add(currentLine);
					currentLine = " > ";
				}
				else if (currentLine.Length + 1 + words[i].Length > lineMaxLength)
				{
					if (currentLine.Trim().EndsWith("//"))
					{
						slideMapping.Add(curSlide);
					}
					else
					{
						slideMapping.Add(curSlide + 1);
					}

					if (currentLine.Contains("//"))
					{
						curSlide++;
					}

					lines.Add(currentLine);
					currentLine = words[i];
				}
				else
				{
					currentLine += " " + words[i];
				}
			}
			if (currentLine.Length > 0)
			{
				lines.Add(currentLine);
			}

			return lines;
		}

		public override bool OnTouchEvent(MotionEvent ev)
		{
			if (mGestureDetector != null)
			{
				return mGestureDetector.OnMotionEvent(ev);
			}
			return false;
		}
		public override bool OnGenericMotionEvent(MotionEvent ev)
		{
			if (mGestureDetector != null)
			{
				return mGestureDetector.OnMotionEvent(ev);
			}
			return false;
		}

		Action scrollAction;
		int scrollRate = 100;

		void CreateLines()
		{
			displayLines = new List<TextView>();

			foreach (var scriptLine in scriptLines)
			{
				var aLabel = new TextView(this);
				aLabel.Text = scriptLine;
				aLabel.SetTextColor(Android.Graphics.Color.Argb(255, 255, 255, 255));
				aLabel.SetTypeface(Typeface.Default, TypefaceStyle.Normal);
				aLabel.SetTextSize(ComplexUnitType.Dip, 30f);
				aLabel.TextScaleX = 0.9f;

				displayLines.Add(aLabel);
				listLayout.AddView(aLabel);
			}

			scrollAction = new Action(() =>
			{
				int delta = 3;

				scrollDelta = scrollTarget - scroller.ScrollY;

				if (scrollDelta > 1)
				{
					scroller.ScrollTo(0, scroller.ScrollY + delta);
				}

				scrollTmr.PostDelayed(scrollAction, 25);
			});
			scrollTmr.PostDelayed(scrollAction, 2000);
		}



		void SetLines()
		{
			int curSlide = slideMapping[curLine];
			AdvanceSlide(curSlide, false);

			scrollTarget = displayLines[curLine].Top;
			scrollDelta = scrollTarget - scroller.ScrollY;

			for (int i = curLine - 1; i > curLine - 6; i--)
			{
				if (i >= 0)
				{
					displayLines[i].SetTextColor(Android.Graphics.Color.Argb(150, 255, 255, 255));
				}
			}
		}


		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			
			Log.Info("GlassPrompter", "boot");

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			scroller = FindViewById<ScrollView>(Resource.Id.scroller);
			listLayout = FindViewById<LinearLayout>(Resource.Id.listLayout);
			layoutRoot = FindViewById<LinearLayout>(Resource.Id.layoutRoot);
			indicator = FindViewById<GridView>(Resource.Id.gridView1);


			scriptLines = getLinesFromScript(fullScript);
			lineCount = scriptLines.Count;
			scriptLines.Add(""); scriptLines.Add(""); scriptLines.Add(""); scriptLines.Add(""); scriptLines.Add(""); scriptLines.Add("");


			//figure out recognizer
			IList<ResolveInfo> services = this.ApplicationContext.PackageManager.QueryIntentServices(
										new Intent(RecognitionService.ServiceInterface), 0);
			if (services.Count == 0)
			{
				return;
			}
			ResolveInfo ri = services[0];
			var pkg = ri.ServiceInfo.PackageName;
			var cls = ri.ServiceInfo.Name;

			speechComp = new ComponentName(pkg, cls);



			CreateLines();
			SetLines();

			mGestureDetector = createGestureDetector(this);

			SetupSpeechReco();
			startReco();
		}



		public void SetupSpeechReco()
		{
			isRecoing = true;

			if (audioManager != null)
			{
				audioManager.Dispose();
				audioManager = null;
			}

			if (speechRecognizer != null)
			{
				speechRecognizer.Dispose();
				speechRecognizer = null;
			}

			if (speechRecognizerIntent != null)
			{
				speechRecognizerIntent.Dispose();
				speechRecognizerIntent = null;
			}

			audioManager = (AudioManager)this.GetSystemService(Context.AudioService);
			speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(this, speechComp);
			speechRecognizer.SetRecognitionListener(new SpeechRecognitionListener(this));
			speechRecognizerIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
			speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
			speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraCallingPackage, this.PackageName);
			speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraPartialResults, true);
			speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, 700);
			speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 1);
			speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 1);
			speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 20);

			isRecoing = false;
		}

		public void startReco()
		{
			if (!isRecoing)
			{
				isRecoing = true;
				Log.Info("GlassPrompter", "About to start reco");
				speechRecognizer.StartListening(speechRecognizerIntent);
				Log.Info("GlassPrompter", "Just started reco");
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (speechRecognizer != null)
			{
				speechRecognizer.Destroy();
			}
		}

		int remoteSlideNum = 0;

		void AdvanceSlide(int newNum, bool force)
		{
			if (!force && newNum == remoteSlideNum) return;
			remoteSlideNum = newNum;

			Log.Info("GlassPrompter", "Advance to slide " + newNum.ToString());

			System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create("https://att-hack.firebaseio.com/curSlide.json");
			req.Method = "PUT";

			WebClient wc = new WebClient();
			wc.UploadStringAsync(new Uri("https://att-hack.firebaseio.com/curSlide.json"), "PUT", (newNum).ToString());

			if (!force)
			{
				(new Handler()).PostDelayed(() => AdvanceSlide(newNum, true), 500);
			}
		}


		class SpeechRecognitionListener : Java.Lang.Object, Android.Speech.IRecognitionListener
		{
			Activity1 parent;
			int waitingOnNetwork = 0;
			Java.Util.Random rnd = new Java.Util.Random();

			public SpeechRecognitionListener(Activity1 parent)
			{
				this.parent = parent;
			}

			public void OnBeginningOfSpeech()
			{
				Log.Info("GlassPrompter", "Speech begin");
				parent.isRecoing = true;


				parent.indicator.SetBackgroundColor(Color.Argb(255, 0, 250, 0));
				waitingOnNetwork = 0;

           
			}

			public void OnBufferReceived(byte[] buffer)
			{
			}


			public void OnEndOfSpeech()
			{
				Log.Info("GlassPrompter", "Speech end");
				parent.isRecoing = false;
				int localWaitingOnNetwork = rnd.NextInt();
				waitingOnNetwork = localWaitingOnNetwork;


				parent.indicator.SetBackgroundColor(Color.Argb(255, 50, 0, 50));

				//if the internet times out, we need to reset the recognizer

				Handler handler = new Handler();
				handler.PostDelayed(() =>
					{
						if (waitingOnNetwork == localWaitingOnNetwork)
						{
							Log.Warn("GlassPrompter", "HIT TIMEOUT  (" + localWaitingOnNetwork.ToString() + ")");
							parent.speechRecognizer.Cancel();
							parent.SetupSpeechReco();
							parent.startReco();
						}
						else
						{
							Log.Info("GlassPrompter", "no timeout");

						}
					}, 5000);

			}

			public void OnError(Android.Speech.SpeechRecognizerError error)
			{
				Log.Info("GlassPrompter", "error: " + error.ToString());
				waitingOnNetwork = 0;
				parent.isRecoing = false;

				parent.indicator.SetBackgroundColor(Color.Argb(255, 150, 15, 0));

				if (error.ToString() == "RecognizerBusy")
				{
					parent.isRecoing = true;
					return;
				}

				parent.startReco();
			}

			public void OnEvent(int eventType, Bundle @params)
			{
				throw new NotImplementedException();
			}

			public void OnPartialResults(Bundle partialResults)
			{
				//sadly this doesn't seem to ever get called.  If I could match on partial results, this could be so much smoother...
			
				Log.Info("GlassPrompter", "partial results");


				String[] results =
	  partialResults.GetStringArray("com.google.android.voicesearch.UNSUPPORTED_PARTIAL_RESULTS");

				if (results == null || results.Length == 0)
				{
					//parent.displayLines[0].Text = "listening...";
					//parent.displayLines[1].Text = "...";
				}
				else if (results.Length == 1)
				{
					//parent.displayLines[0].Text = "~ " + results[0];
					//parent.displayLines[1].Text = "";
				}
				else
				{
					//parent.displayLines[0].Text = "~ " + results[0];
					//parent.displayLines[1].Text = "~ " + results[1];

				}

				throw new NotImplementedException();
			}

			public void OnReadyForSpeech(Bundle @params)
			{
				Log.Info("GlassPrompter", "ready for speech");
				waitingOnNetwork = 0;

				parent.isRecoing = true;
				parent.indicator.SetBackgroundColor(Color.Argb(255, 0, 80, 0));

			}

			public void OnResults(Bundle results)
			{

				Log.Info("GlassPrompter", "results");
				waitingOnNetwork = 0;

				parent.indicator.SetBackgroundColor(Color.Argb(255, 50, 50, 0));


				IList<String> matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);


				if (matches.Count == 0)
				{
					Log.Info("GlassPrompter", "No matches");
				}
				else
				{
					// try to see if anything matches, weighting things that are closer to the current known location
					// match agtain single lines and pairs of lines, since we can't control where the spech reco decides to stop
				
					List<string> texts = new List<string>();
					List<string> sourceMatches = new List<string>();

					List<float> bestScores = new List<float>();
					List<int> lineNum = new List<int>();
					List<float> weights = new List<float>();

					for (int i = 0; i < 6; i++)
					{
						texts.Add(parent.scriptLines[parent.curLine + i]);
						bestScores.Add(0f);
						lineNum.Add(parent.curLine + i);
						weights.Add(5f / ((float)i + 5f));
						sourceMatches.Add("");

						texts.Add(parent.scriptLines[parent.curLine + i] + " " + parent.scriptLines[parent.curLine + i + 1]);
						bestScores.Add(0f);
						lineNum.Add(parent.curLine + i + 1);
						weights.Add(5f / ((float)i + 5.6f));
						sourceMatches.Add("");

					}

					foreach (var match in matches)
					{
						for (int i = 0; i < texts.Count; i++)
						{
							float thisScore = Utils.GetOverlap(texts[i], match) * weights[i];
							if (thisScore > bestScores[i])
							{
								bestScores[i] = thisScore;
								sourceMatches[i] = match;
							}
						}


						Log.Info("GlassPrompter", "Heard ~ '" + match + "'");
					}
					float maxScore = bestScores.Max();
					int bestIndex = bestScores.FindIndex(m => m == maxScore);

					if (maxScore < 0.2)
					{
						Log.Info("GlassPrompter", "Did not match any line. Best score was " + Math.Floor(maxScore * 1000).ToString() + " for '" + sourceMatches[bestIndex] + "' matching '" + texts[bestIndex] + "'");
						parent.indicator.SetBackgroundColor(Color.Argb(255, 50, 50, 0));

					}
					else
					{
						parent.curLine = lineNum[bestIndex] + 1;
						parent.SetLines();
						parent.indicator.SetBackgroundColor(Color.Argb(255, 40, 40, 150));

						Log.Info("GlassPrompter", "Matched. Best score was " + Math.Floor(maxScore * 1000).ToString() + " for '" + sourceMatches[bestIndex] + "' matching '" + texts[bestIndex] + "'");
					}

				}

				parent.startReco();
			}


			public void OnRmsChanged(float rmsdB)
			{
				// maybe show an indicator?

			}



		}



		class GestureDetectorImpl : Java.Lang.Object, Android.Glass.Touchpad.GestureDetector.IBaseListener
		{
			Activity1 parent;

			public GestureDetectorImpl(Activity1 parent)
			{
				this.parent = parent;
			}



			public bool OnGesture(Android.Glass.Touchpad.Gesture p0)
			{
				Log.Info("GlassPrompter", "gesture: " + p0.Name());

				if (p0 == Gesture.Tap)
				{
					parent.curLine += 4;
					parent.SetLines();
					return true;
				}

				return false;

			}
		}
	}

}

