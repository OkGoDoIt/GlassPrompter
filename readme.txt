This is a very rough implementation of a teleprompter for Google Glass.  It is the first real Android app I've ever built, the first Google Glass app, and the first Xamarin app.  I made it in less than 24 hours for the AT&T Developer Summit hackathon in January 2014 in Las Vegas, at which it didn't win, but received a good bit of praise.

My Xamarin trial has expired, so I can't do future work on this until I pony up the $1000 to buy a full license.  In the meantime, hopefully this code is useful to someone with more Glass development experience than me.

Reach me at roger@pincombe.com, http://rogerpincombe.com, or on Twitter as @OkGoDoIt.  Enjoy!


TO USE:
Get Xamarin from http://xamarin.com/download, install Xamarin.Android for Visual Studio.  Get the Xamarin Glass plugin from https://components.xamarin.com/view/googleglass.

You'll need to adb install the included VoiceSearch.apk to your Glass (or any copy of VoiceSearch.apk from JB or newer), as by default Glass doesn't have proper background speech recognition, at least not that I could figure out how to access.  I've found that apk can break the "Listen to..." prompt for on-Glass music listening, so use at your own risk.  You can probably fix it by uninstalling the new VoiceSearch.apk, but I have not tried this yet.