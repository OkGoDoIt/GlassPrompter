using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using System.Net;
using System.Json;
using System.IO;

namespace GlassPrompter
{
	class SpeechReco
	{
		string accessToken = "";

		public void GetToken()
		{
			HttpWebRequest accessTokenRequest = (HttpWebRequest)WebRequest.CreateHttp("https://api.att.com/oauth/token");
			accessTokenRequest.Headers["Content-Type"] = "application/x-www-form-urlencoded";
			string oauthParameters = "grant_type=client_credentials&client_id=aiwhpa92legpodoomkxoijlo3qmiq4dz&client_secret=4s8w2m7ppdukvqmpplmmoyerawax2pgi&scope=STTC";

			UTF8Encoding encoding = new UTF8Encoding();
			byte[] postBytes = encoding.GetBytes(oauthParameters);
			accessTokenRequest.ContentLength = postBytes.Length;

			var postStream = accessTokenRequest.GetRequestStream();
			postStream.Write(postBytes, 0, postBytes.Length);

			WebResponse accessTokenResponse = accessTokenRequest.GetResponse();


			JsonValue json = JsonObject.Load(accessTokenResponse.GetResponseStream());
			accessToken = json["access_token"];


		}

		public void FromFile(string path)
		{
			string speechErrorMessage = "";

			Stream postStream = null;
			FileStream audioFileStream = null;
			audioFileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader reader = new BinaryReader(audioFileStream);
			try
			{

				byte[] binaryData = reader.ReadBytes((int)audioFileStream.Length);
				if (null != binaryData)
				{
					string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
					HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create("https://api.att.com/speech/v3/speechToTextCustom");
					httpRequest.Headers.Add("Authorization", "Bearer " + accessToken);
					httpRequest.Headers.Add("X-SpeechContext", "GrammarList");
					httpRequest.Headers.Add("Content-Language", "en-us");
					httpRequest.ContentType = "multipart/x-srgs-audio; " + "boundary=" + boundary;


					string filenameArgument = "filename";

					string contentType = "audio/wav";

					string data = string.Empty;



					data += "--" + boundary + "\r\n" + "Content-Disposition: form-data; name=\"x-grammar\"";

					//data += "filename=\"prefix.srgs\" ";

					string xgrammerContent = @"<grammar root=""top"" xml:lang=""en-US""> 
  <rule id=""WORD""> 
      <one-of> 
        <item weight=""17"">hello</item> 
        <item weight=""16"">this</item> 
		<item weight=""15"">is</item> 
		<item weight=""14"">a</item> 
		<item weight=""13"">test</item> 
		<item weight=""12"">of</item> 
		<item weight=""11"">the</item>
		<item weight=""10"">microphone</item> 		
      </one-of> 
  </rule> 
  <rule id=""top"" scope=""public""> 
	  <item repeat=""2-10"">
		<ruleref uri=""#WORD""/>
	  </item>
  </rule> 
</grammar>";

					data += "\r\nContent-Type: application/srgs+xml \r\n" + "\r\n" + xgrammerContent + "\r\n\r\n\r\n" + "--" + boundary + "\r\n";

					data += "Content-Disposition: form-data; name=\"x-voice\"; " + filenameArgument + "=\"" + filenameArgument + ".wav\"";
					data += "\r\nContent-Type: " + contentType + "\r\n\r\n";
					UTF8Encoding encoding = new UTF8Encoding();
					byte[] firstPart = encoding.GetBytes(data);
					int newSize = firstPart.Length + binaryData.Length;

					var memoryStream = new MemoryStream(new byte[newSize], 0, newSize, true, true);
					memoryStream.Write(firstPart, 0, firstPart.Length);
					memoryStream.Write(binaryData, 0, binaryData.Length);

					byte[] postBytes = memoryStream.GetBuffer();

					byte[] byteLastBoundary = encoding.GetBytes("\r\n\r\n" + "--" + boundary + "--");
					int totalSize = postBytes.Length + byteLastBoundary.Length;

					var totalMS = new MemoryStream(new byte[totalSize], 0, totalSize, true, true);
					totalMS.Write(postBytes, 0, postBytes.Length);
					totalMS.Write(byteLastBoundary, 0, byteLastBoundary.Length);

					byte[] finalpostBytes = totalMS.GetBuffer();

					httpRequest.ContentLength = totalMS.Length;
					//httpRequest.ContentType = contentType;
					httpRequest.Accept = "application/json";
					httpRequest.Method = "POST";
					httpRequest.KeepAlive = true;
					postStream = httpRequest.GetRequestStream();
					postStream.Write(finalpostBytes, 0, finalpostBytes.Length);
					postStream.Close();

					HttpWebResponse speechResponse = (HttpWebResponse)httpRequest.GetResponse();
					using (StreamReader streamReader = new StreamReader(speechResponse.GetResponseStream()))
					{
						string speechRequestResponse = streamReader.ReadToEnd();
						if (!string.IsNullOrEmpty(speechRequestResponse))
						{
							Console.Write(speechRequestResponse);


						}
						else
						{
							speechErrorMessage = "??";
						}

						streamReader.Close();
					}
				}
				else
				{
					speechErrorMessage = "Empty speech to text response";
				}
			}
			catch (WebException we)
			{
				string errorResponse = string.Empty;

				try
				{
					using (StreamReader sr2 = new StreamReader(we.Response.GetResponseStream()))
					{
						errorResponse = sr2.ReadToEnd();
						sr2.Close();
					}
				}
				catch
				{
					errorResponse = "Unable to get response";
				}

				speechErrorMessage = errorResponse;
			}
			catch (Exception ex)
			{
				speechErrorMessage = ex.ToString();
			}
			finally
			{
				reader.Close();
				audioFileStream.Close();
				if (null != postStream)
				{
					postStream.Close();
				}
			}
		}
	}

	private class OauthRespJson
	{
		public class RootObject
		{
			public string access_token { get; set; }
			public string token_type { get; set; }
			public int expires_in { get; set; }
			public string refresh_token { get; set; }
		}
	}
}