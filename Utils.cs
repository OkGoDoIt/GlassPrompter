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
using System.Text.RegularExpressions;

namespace GlassPrompter
{
	public static class Utils
	{


		private const string PUNCT = "\\p{Punct}";
		private const string SPACE = "\\p{Space}";

		private static Regex justWords = new Regex(@"[a-zA-Z0-9'-]+", RegexOptions.Compiled);

		public static float GetOverlap(string target, string input)
		{
			string[] wTarget = justWords.Matches(target).Cast<Match>().Select(m => m.Value.ToLower()).Where(w => w.Length > 2).Distinct().ToArray();
			string[] wInput = justWords.Matches(input).Cast<Match>().Select(m => m.Value.ToLower()).Where(w => w.Length > 2).Distinct().ToArray();

			int overlap = wTarget.Intersect(wInput).Count();
			if (overlap == 0 || wTarget.Length == 0) return 0f;

			return (((float)overlap) / ((float)wTarget.Length));
		}

	}
}