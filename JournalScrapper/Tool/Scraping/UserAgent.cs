namespace JournalScrapper.Tool.Scraping
{
    public class UserAgent
    {
        public string GetRandomUserAgent()
        {
            var agents = new List<string>
            {
                // chrome - windows
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/118.0.0.0 safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/121.0.0.0 safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/122.0.0.0 safari/537.36",
                "mozilla/5.0 (windows nt 11.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/123.0.0.0 safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/124.0.0.0 safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/139.0.0.0 safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/140.0.0.0 safari/537.36",

                // chrome - macos
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36",
                "mozilla/5.0 (macintosh; intel mac os x 14_0) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 safari/537.36",
                "mozilla/5.0 (macintosh; intel mac os x 13_6) applewebkit/537.36 (khtml, like gecko) chrome/118.0.0.0 safari/537.36",
                "mozilla/5.0 (macintosh; intel mac os x 14_2) applewebkit/537.36 (khtml, like gecko) chrome/121.0.0.0 safari/537.36",
                "mozilla/5.0 (macintosh; intel mac os x 14_3) applewebkit/537.36 (khtml, like gecko) chrome/122.0.0.0 safari/537.36",
                "mozilla/5.0 (macintosh; intel mac os x 14_4) applewebkit/537.36 (khtml, like gecko) chrome/123.0.0.0 safari/537.36",
                "mozilla/5.0 (macintosh; intel mac os x 14_5) applewebkit/537.36 (khtml, like gecko) chrome/124.0.0.0 safari/537.36",
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36",
                "mozilla/5.0 (macintosh; intel mac os x 14_2) applewebkit/537.36 (khtml, like gecko) chrome/139.0.0.0 safari/537.36",

                // chrome - linux
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 safari/537.36",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/118.0.0.0 safari/537.36",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/121.0.0.0 safari/537.36",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/122.0.0.0 safari/537.36",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/123.0.0.0 safari/537.36",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/139.0.0.0 safari/537.36",

                // firefox - windows
                "mozilla/5.0 (windows nt 10.0; win64; x64; rv:121.0) gecko/20100101 firefox/121.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64; rv:120.0) gecko/20100101 firefox/120.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64; rv:119.0) gecko/20100101 firefox/119.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64; rv:122.0) gecko/20100101 firefox/122.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64; rv:123.0) gecko/20100101 firefox/123.0",
                "mozilla/5.0 (windows nt 11.0; win64; x64; rv:121.0) gecko/20100101 firefox/121.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64; rv:124.0) gecko/20100101 firefox/124.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64; rv:125.0) gecko/20100101 firefox/125.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64; rv:126.0) gecko/20100101 firefox/126.0",

                // firefox - macos
                "mozilla/5.0 (macintosh; intel mac os x 14.1; rv:121.0) gecko/20100101 firefox/121.0",
                "mozilla/5.0 (macintosh; intel mac os x 14.0; rv:120.0) gecko/20100101 firefox/120.0",
                "mozilla/5.0 (macintosh; intel mac os x 13.6; rv:119.0) gecko/20100101 firefox/119.0",
                "mozilla/5.0 (macintosh; intel mac os x 14.2; rv:122.0) gecko/20100101 firefox/122.0",
                "mozilla/5.0 (macintosh; intel mac os x 14.3; rv:123.0) gecko/20100101 firefox/123.0",
                "mozilla/5.0 (macintosh; intel mac os x 14.4; rv:124.0) gecko/20100101 firefox/124.0",
                "mozilla/5.0 (macintosh; intel mac os x 14.5; rv:125.0) gecko/20100101 firefox/125.0",

                // firefox - linux
                "mozilla/5.0 (x11; ubuntu; linux x86_64; rv:121.0) gecko/20100101 firefox/121.0",
                "mozilla/5.0 (x11; ubuntu; linux x86_64; rv:120.0) gecko/20100101 firefox/120.0",
                "mozilla/5.0 (x11; ubuntu; linux x86_64; rv:119.0) gecko/20100101 firefox/119.0",
                "mozilla/5.0 (x11; ubuntu; linux x86_64; rv:122.0) gecko/20100101 firefox/122.0",
                "mozilla/5.0 (x11; ubuntu; linux x86_64; rv:123.0) gecko/20100101 firefox/123.0",
                "mozilla/5.0 (x11; ubuntu; linux x86_64; rv:124.0) gecko/20100101 firefox/124.0",
                "mozilla/5.0 (x11; ubuntu; linux x86_64; rv:125.0) gecko/20100101 firefox/125.0",

                // safari - macos
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/605.1.15 (khtml, like gecko) version/17.1 safari/605.1.15",
                "mozilla/5.0 (macintosh; intel mac os x 14_0) applewebkit/605.1.15 (khtml, like gecko) version/16.6 safari/605.1.15",
                "mozilla/5.0 (macintosh; intel mac os x 13_6) applewebkit/605.1.15 (khtml, like gecko) version/15.6.1 safari/605.1.15",
                "mozilla/5.0 (macintosh; intel mac os x 14_2) applewebkit/605.1.15 (khtml, like gecko) version/17.2 safari/605.1.15",
                "mozilla/5.0 (macintosh; intel mac os x 14_3) applewebkit/605.1.15 (khtml, like gecko) version/17.3 safari/605.1.15",
                "mozilla/5.0 (macintosh; intel mac os x 14_4) applewebkit/605.1.15 (khtml, like gecko) version/17.4 safari/605.1.15",
                "mozilla/5.0 (macintosh; intel mac os x 14_5) applewebkit/605.1.15 (khtml, like gecko) version/17.5 safari/605.1.15",

                // safari - ios
                "mozilla/5.0 (iphone; cpu iphone os 17_1 like mac os x) applewebkit/605.1.15 (khtml, like gecko) version/17.0 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 16_6 like mac os x) applewebkit/605.1.15 (khtml, like gecko) version/16.0 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 15_6 like mac os x) applewebkit/605.1.15 (khtml, like gecko) version/15.0 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 17_2 like mac os x) applewebkit/605.1.15 (khtml, like gecko) version/17.2 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 17_3 like mac os x) applewebkit/605.1.15 (khtml, like gecko) version/17.3 mobile/15e148 safari/604.1",
                "mozilla/5.0 (ipad; cpu os 17_1 like mac os x) applewebkit/605.1.15 (khtml, like gecko) version/17.0 mobile/15e148 safari/604.1",
                "mozilla/5.0 (ipad; cpu os 16_6 like mac os x) applewebkit/605.1.15 (khtml, like gecko) version/16.0 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 17_4 like mac os x) applewebkit/605.1.15 (khtml, like gecko) version/17.4 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 17_5 like mac os x) applewebkit/605.1.15 (khtml, like gecko) version/17.5 mobile/15e148 safari/604.1",

                // edge - windows
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 edg/120.0.2210.91",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 safari/537.36 edg/119.0.2151.97",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/118.0.0.0 safari/537.36 edg/118.0.2088.76",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/121.0.0.0 safari/537.36 edg/121.0.2277.83",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/122.0.0.0 safari/537.36 edg/122.0.2365.63",
                "mozilla/5.0 (windows nt 11.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 edg/120.0.2210.91",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/123.0.0.0 safari/537.36 edg/123.0.2420.81",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/124.0.0.0 safari/537.36 edg/124.0.2478.51",

                // edge - macos
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 edg/120.0.2210.91",
                "mozilla/5.0 (macintosh; intel mac os x 14_0) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 safari/537.36 edg/119.0.2151.97",
                "mozilla/5.0 (macintosh; intel mac os x 13_6) applewebkit/537.36 (khtml, like gecko) chrome/118.0.0.0 safari/537.36 edg/118.0.2088.76",
                "mozilla/5.0 (macintosh; intel mac os x 14_2) applewebkit/537.36 (khtml, like gecko) chrome/121.0.0.0 safari/537.36 edg/121.0.2277.83",
                "mozilla/5.0 (macintosh; intel mac os x 14_3) applewebkit/537.36 (khtml, like gecko) chrome/122.0.0.0 safari/537.36 edg/122.0.2365.63",
                "mozilla/5.0 (macintosh; intel mac os x 14_4) applewebkit/537.36 (khtml, like gecko) chrome/123.0.0.0 safari/537.36 edg/123.0.2420.81",

                // android - chrome
                "mozilla/5.0 (linux; android 13; sm-a536b) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 mobile safari/537.36",
                "mozilla/5.0 (linux; android 12; sm-a525f) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 mobile safari/537.36",
                "mozilla/5.0 (linux; android 11; sm-a515f) applewebkit/537.36 (khtml, like gecko) chrome/118.0.0.0 mobile safari/537.36",
                "mozilla/5.0 (linux; android 14; sm-g998b) applewebkit/537.36 (khtml, like gecko) chrome/121.0.0.0 mobile safari/537.36",
                "mozilla/5.0 (linux; android 13; pixel 7) applewebkit/537.36 (khtml, like gecko) chrome/122.0.0.0 mobile safari/537.36",
                "mozilla/5.0 (linux; android 12; pixel 6) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 mobile safari/537.36",
                "mozilla/5.0 (linux; android 14; sm-s928b) applewebkit/537.36 (khtml, like gecko) chrome/123.0.0.0 mobile safari/537.36",
                "mozilla/5.0 (linux; android 15; sm-a536b) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 mobile safari/537.36",

                // android - firefox
                "mozilla/5.0 (android 13; mobile; rv:121.0) gecko/121.0 firefox/121.0",
                "mozilla/5.0 (android 12; mobile; rv:120.0) gecko/120.0 firefox/120.0",
                "mozilla/5.0 (android 14; mobile; rv:122.0) gecko/122.0 firefox/122.0",
                "mozilla/5.0 (android 13; mobile; rv:123.0) gecko/123.0 firefox/123.0",
                "mozilla/5.0 (android 15; mobile; rv:125.0) gecko/125.0 firefox/125.0",

                // ios - chrome
                "mozilla/5.0 (iphone; cpu iphone os 17_1 like mac os x) applewebkit/605.1.15 (khtml, like gecko) crios/120.0.6099.119 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 16_6 like mac os x) applewebkit/605.1.15 (khtml, like gecko) crios/119.0.6045.169 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 15_6 like mac os x) applewebkit/605.1.15 (khtml, like gecko) crios/118.0.5993.89 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 17_2 like mac os x) applewebkit/605.1.15 (khtml, like gecko) crios/121.0.6167.138 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 17_3 like mac os x) applewebkit/605.1.15 (khtml, like gecko) crios/122.0.6261.89 mobile/15e148 safari/604.1",
                "mozilla/5.0 (ipad; cpu os 17_1 like mac os x) applewebkit/605.1.15 (khtml, like gecko) crios/120.0.6099.119 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 17_4 like mac os x) applewebkit/605.1.15 (khtml, like gecko) crios/123.0.6312.52 mobile/15e148 safari/604.1",

                // ios - firefox
                "mozilla/5.0 (iphone; cpu iphone os 17_1 like mac os x) applewebkit/605.1.15 (khtml, like gecko) fxios/121.0 mobile/15e148 safari/605.1.15",
                "mozilla/5.0 (iphone; cpu iphone os 16_6 like mac os x) applewebkit/605.1.15 (khtml, like gecko) fxios/120.0 mobile/15e148 safari/605.1.15",
                "mozilla/5.0 (iphone; cpu iphone os 17_2 like mac os x) applewebkit/605.1.15 (khtml, like gecko) fxios/122.0 mobile/15e148 safari/605.1.15",
                "mozilla/5.0 (iphone; cpu iphone os 17_3 like mac os x) applewebkit/605.1.15 (khtml, like gecko) fxios/123.0 mobile/15e148 safari/605.1.15",
                "mozilla/5.0 (iphone; cpu iphone os 17_4 like mac os x) applewebkit/605.1.15 (khtml, like gecko) fxios/124.0 mobile/15e148 safari/605.1.15",

                // opera - windows
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 opr/106.0.0.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 safari/537.36 opr/105.0.0.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/118.0.0.0 safari/537.36 opr/104.0.0.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/121.0.0.0 safari/537.36 opr/107.0.0.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/122.0.0.0 safari/537.36 opr/108.0.0.0",

                // opera - macos
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 opr/106.0.0.0",
                "mozilla/5.0 (macintosh; intel mac os x 14_0) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 safari/537.36 opr/105.0.0.0",
                "mozilla/5.0 (macintosh; intel mac os x 13_6) applewebkit/537.36 (khtml, like gecko) chrome/118.0.0.0 safari/537.36 opr/104.0.0.0",
                "mozilla/5.0 (macintosh; intel mac os x 14_2) applewebkit/537.36 (khtml, like gecko) chrome/121.0.0.0 safari/537.36 opr/107.0.0.0",

                // opera - linux
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 opr/106.0.0.0",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 safari/537.36 opr/105.0.0.0",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/118.0.0.0 safari/537.36 opr/104.0.0.0",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/121.0.0.0 safari/537.36 opr/107.0.0.0",

                // opera - android
                "mozilla/5.0 (linux; android 13; sm-a536b) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 mobile safari/537.36 opr/85.0.2254.60649",
                "mozilla/5.0 (linux; android 12; sm-a525f) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 mobile safari/537.36 opr/84.0.2254.59826",
                "mozilla/5.0 (linux; android 14; sm-g998b) applewebkit/537.36 (khtml, like gecko) chrome/121.0.0.0 mobile safari/537.36 opr/86.0.2254.61213",

                // ios - opera
                "mozilla/5.0 (iphone; cpu iphone os 17_1 like mac os x) applewebkit/605.1.15 (khtml, like gecko) opios/75.0.4054.64 mobile/15e148 safari/605.1.15",
                "mozilla/5.0 (iphone; cpu iphone os 16_6 like mac os x) applewebkit/605.1.15 (khtml, like gecko) opios/74.0.3729.169 mobile/15e148 safari/605.1.15",
                "mozilla/5.0 (iphone; cpu iphone os 17_2 like mac os x) applewebkit/605.1.15 (khtml, like gecko) opios/76.0.3809.84 mobile/15e148 safari/605.1.15",

                // brave - windows
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 brave/1.61.109",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 safari/537.36 brave/1.60.118",

                // brave - macos
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 brave/1.61.109",

                // brave - linux
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 brave/1.61.109",

                // brave - android
                "mozilla/5.0 (linux; android 13; sm-a536b) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 mobile safari/537.36 brave/1.61.109",

                // brave - ios
                "mozilla/5.0 (iphone; cpu iphone os 17_1 like mac os x) applewebkit/605.1.15 (khtml, like gecko) brave/1.61.109 mobile/15e148 safari/605.1.15",

                // vivaldi - windows
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 vivaldi/6.5.3206.53",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 safari/537.36 vivaldi/6.4.3160.42",

                // vivaldi - macos
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 vivaldi/6.5.3206.53",

                // vivaldi - linux
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 vivaldi/6.5.3206.53",

                // yandex browser - windows
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 yabrowser/24.1.0 yaweb/24.1.0 safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/119.0.0.0 yabrowser/23.11.0 yaweb/23.11.0 safari/537.36",

                // yandex browser - macos
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 yabrowser/24.1.0 yaweb/24.1.0 safari/537.36",

                // yandex browser - android
                "mozilla/5.0 (linux; android 13; sm-a536b) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 mobile yabrowser/24.1.0 yaweb/24.1.0 safari/537.36",

                // uc browser - android
                "mozilla/5.0 (linux; u; android 13; en_us; sm-a536b) applewebkit/537.36 (khtml, like gecko) version/4.0 chrome/120.0.0.0 ucbrowser/13.4.5.1306 ucbs/2.11.0.22 mobile safari/537.36",
                "mozilla/5.0 (linux; u; android 12; en_us; sm-a525f) applewebkit/537.36 (khtml, like gecko) version/4.0 chrome/119.0.0.0 ucbrowser/13.4.2.1303 ucbs/2.11.0.22 mobile safari/537.36",

                // uc browser - windows
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 ucbrowser/7.0.185.1002",

                // samsung internet - android
                "mozilla/5.0 (linux; android 13; sm-a536b) applewebkit/537.36 (khtml, like gecko) samsungbrowser/23.0 chrome/120.0.0.0 mobile safari/537.36",
                "mozilla/5.0 (linux; android 12; sm-a525f) applewebkit/537.36 (khtml, like gecko) samsungbrowser/22.0 chrome/119.0.0.0 mobile safari/537.36",

                // duckduckgo - windows
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 duckduckgo/5.198.1",

                // duckduckgo - macos
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 duckduckgo/5.198.1",

                // arc browser - macos
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 arc/1.20.0",

                // arc browser - windows
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/120.0.0.0 safari/537.36 arc/1.20.0",

                // additional from 2025 sources
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36",
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64; rv:125.0) gecko/20100101 firefox/125.0",
                "mozilla/5.0 (macintosh; intel mac os x 14.1; rv:125.0) gecko/20100101 firefox/125.0",
                "mozilla/5.0 (x11; ubuntu; linux x86_64; rv:125.0) gecko/20100101 firefox/125.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36 edg/138.0.2310.54",
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36 edg/138.0.2310.54",
                "mozilla/5.0 (linux; android 15; sm-a536b) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 mobile safari/537.36",
                "mozilla/5.0 (android 15; mobile; rv:125.0) gecko/125.0 firefox/125.0",
                "mozilla/5.0 (iphone; cpu iphone os 18_0 like mac os x) applewebkit/605.1.15 (khtml, like gecko) version/18.0 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 18_0 like mac os x) applewebkit/605.1.15 (khtml, like gecko) crios/138.0.6099.119 mobile/15e148 safari/604.1",
                "mozilla/5.0 (iphone; cpu iphone os 18_0 like mac os x) applewebkit/605.1.15 (khtml, like gecko) fxios/125.0 mobile/15e148 safari/605.1.15",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36 vivaldi/7.0.3106.54",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 yabrowser/25.1.0 yaweb/25.1.0 safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36 brave/1.71.109",
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36 brave/1.71.109",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36 brave/1.71.109",
                "mozilla/5.0 (linux; android 15; sm-a536b) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 mobile safari/537.36 brave/1.71.109",
                "mozilla/5.0 (iphone; cpu iphone os 18_0 like mac os x) applewebkit/605.1.15 (khtml, like gecko) brave/1.71.109 mobile/15e148 safari/605.1.15",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36 duckduckgo/5.198.1",
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36 arc/1.20.0",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36 ucbrowser/8.0.198.104",
                "mozilla/5.0 (linux; android 15; sm-a536b) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 mobile safari/537.36 samsungbrowser/25.0",
                "mozilla/5.0 (linux; android 15; sm-a536b) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 mobile yabrowser/25.1.0 yaweb/25.1.0 safari/537.36",
                "mozilla/5.0 (linux; u; android 15; en_us; sm-a536b) applewebkit/537.36 (khtml, like gecko) version/4.0 chrome/138.0.0.0 ucbrowser/14.0.0.1306 ucbs/2.11.0.22 mobile safari/537.36",
                "mozilla/5.0 (windows nt 10.0; win64; x64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36 opr/124.0.0.0",
                "mozilla/5.0 (macintosh; intel mac os x 14_1) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36 opr/124.0.0.0",
                "mozilla/5.0 (x11; linux x86_64) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 safari/537.36 opr/124.0.0.0",
                "mozilla/5.0 (linux; android 15; sm-a536b) applewebkit/537.36 (khtml, like gecko) chrome/138.0.0.0 mobile safari/537.36 opr/90.0.2254.60649"
            };
            return agents[new Random((int)DateTime.Now.Ticks).Next(agents.Count)];
        }
    }
}
