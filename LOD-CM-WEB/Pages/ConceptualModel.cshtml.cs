using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LOD_CM.Pages
{
    public class ConceptualModel : PageModel
    {
        //public DatasetForIndex Dataset{ get; private set; }
        public string ImageContent { get; private set; }
        public IList<string> Properties { get; private set; }
        public string Message { get; private set; }
        [HiddenInput]
        public string ClassName { get; private set; }

        [HiddenInput]
        public int Threshold { get; private set; }

        [HiddenInput]
        public string DatasetLabel { get; private set; }
        public async Task OnGetAsync(DatasetForIndex dataset)
        {
            // TODO: display several MFP and let user choose
            var mainDir = @"E:\download";
            var directory = Path.Combine(mainDir, dataset.Label, 
                dataset.Class, dataset.Threshold.ToString());
            var images = new List<string>();
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory);
                foreach (var file in files)
                {
                    if (file.EndsWith("svg"))
                    {
                        images.Add(await System.IO.File.ReadAllTextAsync(file));
                    }
                    else if (file.EndsWith("json"))
                    {

                    }
                    else if (file.Equals("mfp.txt"))
                    {
                        
                    }
                }
            }
            // Dataset = dataset;
            ImageContent = @"<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" contentScriptType=""application/ecmascript"" contentStyleType=""text/css"" height=""713px"" preserveAspectRatio=""none"" style=""width:495px;height:713px;"" version=""1.1"" viewBox=""0 0 495 713"" width=""495px"" zoomAndPan=""magnify""><defs>
                <filter height=""300%"" id=""fufz74as0n034"" width=""300%"" x=""-1"" y=""-1""><feGaussianBlur result=""blurOut"" stdDeviation=""2.0""/><feColorMatrix in=""blurOut"" result=""blurOut2"" type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 .4 0""/><feOffset dx=""4.0"" dy=""4.0"" in=""blurOut2"" result=""blurOut3""/><feBlend in=""SourceGraphic"" in2=""blurOut3"" mode=""normal""/></filter></defs>
                <g><!--class Film--><rect fill=""#FEFECE"" filter=""url(#fufz74as0n034)"" height=""86.4141"" id=""Film"" style=""stroke: #A80036; stroke-width: 1.5;"" width=""223"" x=""59.5"" y=""273""/><ellipse cx=""154.75"" cy=""289"" fill=""#ADD1B2"" rx=""11"" ry=""11"" style=""stroke: #A80036; stroke-width: 1.0;""/><path d=""M157.0938,284.6719 L157.2656,284.75 C157.4844,284.4375 157.6875,284.3438 157.9844,284.3438 C158.2813,284.3438 158.5625,284.4844 158.7188,284.75 C158.8125,284.9063 158.8281,285.0313 158.8281,285.4688 L158.8281,286.8906 C158.8281,287.3125 158.7969,287.5 158.6875,287.6563 C158.5156,287.875 158.25,288.0156 157.9844,288.0156 C157.7656,288.0156 157.5313,287.9063 157.3906,287.7656 C157.25,287.6406 157.2188,287.5156 157.1563,287.1094 C157.0625,286.7031 156.8906,286.4844 156.4063,286.2031 C155.9375,285.9531 155.3281,285.7969 154.75,285.7969 C153.0156,285.7969 151.7656,287.1094 151.7656,288.8906 L151.7656,289.9844 C151.7656,291.6875 153.0625,292.7813 155.1094,292.7813 C155.875,292.7813 156.5625,292.6563 156.9844,292.3906 C157.1719,292.2969 157.1719,292.2969 157.625,291.8125 C157.8125,291.625 158.0156,291.5469 158.2344,291.5469 C158.7031,291.5469 159.0938,291.9375 159.0938,292.3906 C159.0938,292.7813 158.7656,293.2344 158.1875,293.6406 C157.4375,294.1875 156.2813,294.4844 155.0625,294.4844 C152.1719,294.4844 150.0625,292.5938 150.0625,290.0156 L150.0625,288.8906 C150.0625,286.1719 152.0625,284.0938 154.6875,284.0938 C155.5625,284.0938 156.1563,284.2344 157.0938,284.6719 Z ""/><text fill=""#000000"" font-family=""sans-serif"" font-size=""12"" lengthAdjust=""spacingAndGlyphs"" textLength=""24"" x=""175.25"" y=""293.1543"">Film</text><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""60.5"" x2=""281.5"" y1=""305"" y2=""305""/><text fill=""#000000"" font-family=""sans-serif"" font-size=""11"" lengthAdjust=""spacingAndGlyphs"" textLength=""211"" x=""65.5"" y=""319.2104"">runtime:XMLSchema#double sup=71</text><text fill=""#000000"" font-family=""sans-serif"" font-size=""11"" lengthAdjust=""spacingAndGlyphs"" textLength=""72"" x=""65.5"" y=""332.0151"">type sup=97</text><text fill=""#000000"" font-family=""sans-serif"" font-size=""11"" lengthAdjust=""spacingAndGlyphs"" textLength=""75"" x=""65.5"" y=""344.8198"">label sup=97</text><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""60.5"" x2=""281.5"" y1=""351.4141"" y2=""351.4141""/><!--class Person--><rect fill=""#FEFECE"" filter=""url(#fufz74as0n034)"" height=""48"" id=""Person"" style=""stroke: #A80036; stroke-width: 1.5;"" width=""76"" x=""227"" y=""436""/><ellipse cx=""242"" cy=""452"" fill=""#ADD1B2"" rx=""11"" ry=""11"" style=""stroke: #A80036; stroke-width: 1.0;""/><path d=""M244.3438,447.6719 L244.5156,447.75 C244.7344,447.4375 244.9375,447.3438 245.2344,447.3438 C245.5313,447.3438 245.8125,447.4844 245.9688,447.75 C246.0625,447.9063 246.0781,448.0313 246.0781,448.4688 L246.0781,449.8906 C246.0781,450.3125 246.0469,450.5 245.9375,450.6563 C245.7656,450.875 245.5,451.0156 245.2344,451.0156 C245.0156,451.0156 244.7813,450.9063 244.6406,450.7656 C244.5,450.6406 244.4688,450.5156 244.4063,450.1094 C244.3125,449.7031 244.1406,449.4844 243.6563,449.2031 C243.1875,448.9531 242.5781,448.7969 242,448.7969 C240.2656,448.7969 239.0156,450.1094 239.0156,451.8906 L239.0156,452.9844 C239.0156,454.6875 240.3125,455.7813 242.3594,455.7813 C243.125,455.7813 243.8125,455.6563 244.2344,455.3906 C244.4219,455.2969 244.4219,455.2969 244.875,454.8125 C245.0625,454.625 245.2656,454.5469 245.4844,454.5469 C245.9531,454.5469 246.3438,454.9375 246.3438,455.3906 C246.3438,455.7813 246.0156,456.2344 245.4375,456.6406 C244.6875,457.1875 243.5313,457.4844 242.3125,457.4844 C239.4219,457.4844 237.3125,455.5938 237.3125,453.0156 L237.3125,451.8906 C237.3125,449.1719 239.3125,447.0938 241.9375,447.0938 C242.8125,447.0938 243.4063,447.2344 244.3438,447.6719 Z ""/><text fill=""#000000"" font-family=""sans-serif"" font-size=""12"" lengthAdjust=""spacingAndGlyphs"" textLength=""44"" x=""256"" y=""456.1543"">Person</text><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""228"" x2=""302"" y1=""468"" y2=""468""/><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""228"" x2=""302"" y1=""476"" y2=""476""/><!--class Work--><rect fill=""#FEFECE"" filter=""url(#fufz74as0n034)"" height=""48"" id=""Work"" style=""stroke: #A80036; stroke-width: 1.5;"" width=""63"" x=""240.5"" y=""117""/><ellipse cx=""255.5"" cy=""133"" fill=""#ADD1B2"" rx=""11"" ry=""11"" style=""stroke: #A80036; stroke-width: 1.0;""/><path d=""M257.8438,128.6719 L258.0156,128.75 C258.2344,128.4375 258.4375,128.3438 258.7344,128.3438 C259.0313,128.3438 259.3125,128.4844 259.4688,128.75 C259.5625,128.9063 259.5781,129.0313 259.5781,129.4688 L259.5781,130.8906 C259.5781,131.3125 259.5469,131.5 259.4375,131.6563 C259.2656,131.875 259,132.0156 258.7344,132.0156 C258.5156,132.0156 258.2813,131.9063 258.1406,131.7656 C258,131.6406 257.9688,131.5156 257.9063,131.1094 C257.8125,130.7031 257.6406,130.4844 257.1563,130.2031 C256.6875,129.9531 256.0781,129.7969 255.5,129.7969 C253.7656,129.7969 252.5156,131.1094 252.5156,132.8906 L252.5156,133.9844 C252.5156,135.6875 253.8125,136.7813 255.8594,136.7813 C256.625,136.7813 257.3125,136.6563 257.7344,136.3906 C257.9219,136.2969 257.9219,136.2969 258.375,135.8125 C258.5625,135.625 258.7656,135.5469 258.9844,135.5469 C259.4531,135.5469 259.8438,135.9375 259.8438,136.3906 C259.8438,136.7813 259.5156,137.2344 258.9375,137.6406 C258.1875,138.1875 257.0313,138.4844 255.8125,138.4844 C252.9219,138.4844 250.8125,136.5938 250.8125,134.0156 L250.8125,132.8906 C250.8125,130.1719 252.8125,128.0938 255.4375,128.0938 C256.3125,128.0938 256.9063,128.2344 257.8438,128.6719 Z ""/><text fill=""#000000"" font-family=""sans-serif"" font-size=""12"" lengthAdjust=""spacingAndGlyphs"" textLength=""31"" x=""269.5"" y=""137.1543"">Work</text><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""241.5"" x2=""302.5"" y1=""149"" y2=""149""/><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""241.5"" x2=""302.5"" y1=""157"" y2=""157""/><!--class Actor--><rect fill=""#FEFECE"" filter=""url(#fufz74as0n034)"" height=""48"" id=""Actor"" style=""stroke: #A80036; stroke-width: 1.5;"" width=""65"" x=""15.5"" y=""654""/><ellipse cx=""30.5"" cy=""670"" fill=""#ADD1B2"" rx=""11"" ry=""11"" style=""stroke: #A80036; stroke-width: 1.0;""/><path d=""M32.8438,665.6719 L33.0156,665.75 C33.2344,665.4375 33.4375,665.3438 33.7344,665.3438 C34.0313,665.3438 34.3125,665.4844 34.4688,665.75 C34.5625,665.9063 34.5781,666.0313 34.5781,666.4688 L34.5781,667.8906 C34.5781,668.3125 34.5469,668.5 34.4375,668.6563 C34.2656,668.875 34,669.0156 33.7344,669.0156 C33.5156,669.0156 33.2813,668.9063 33.1406,668.7656 C33,668.6406 32.9688,668.5156 32.9063,668.1094 C32.8125,667.7031 32.6406,667.4844 32.1563,667.2031 C31.6875,666.9531 31.0781,666.7969 30.5,666.7969 C28.7656,666.7969 27.5156,668.1094 27.5156,669.8906 L27.5156,670.9844 C27.5156,672.6875 28.8125,673.7813 30.8594,673.7813 C31.625,673.7813 32.3125,673.6563 32.7344,673.3906 C32.9219,673.2969 32.9219,673.2969 33.375,672.8125 C33.5625,672.625 33.7656,672.5469 33.9844,672.5469 C34.4531,672.5469 34.8438,672.9375 34.8438,673.3906 C34.8438,673.7813 34.5156,674.2344 33.9375,674.6406 C33.1875,675.1875 32.0313,675.4844 30.8125,675.4844 C27.9219,675.4844 25.8125,673.5938 25.8125,671.0156 L25.8125,669.8906 C25.8125,667.1719 27.8125,665.0938 30.4375,665.0938 C31.3125,665.0938 31.9063,665.2344 32.8438,665.6719 Z ""/><text fill=""#000000"" font-family=""sans-serif"" font-size=""12"" lengthAdjust=""spacingAndGlyphs"" textLength=""33"" x=""44.5"" y=""674.1543"">Actor</text><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""16.5"" x2=""79.5"" y1=""686"" y2=""686""/><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""16.5"" x2=""79.5"" y1=""694"" y2=""694""/><!--class Thing--><rect fill=""#FEFECE"" filter=""url(#fufz74as0n034)"" height=""48"" id=""Thing"" style=""stroke: #A80036; stroke-width: 1.5;"" width=""66"" x=""339"" y=""8""/><ellipse cx=""354"" cy=""24"" fill=""#ADD1B2"" rx=""11"" ry=""11"" style=""stroke: #A80036; stroke-width: 1.0;""/><path d=""M356.3438,19.6719 L356.5156,19.75 C356.7344,19.4375 356.9375,19.3438 357.2344,19.3438 C357.5313,19.3438 357.8125,19.4844 357.9688,19.75 C358.0625,19.9063 358.0781,20.0313 358.0781,20.4688 L358.0781,21.8906 C358.0781,22.3125 358.0469,22.5 357.9375,22.6563 C357.7656,22.875 357.5,23.0156 357.2344,23.0156 C357.0156,23.0156 356.7813,22.9063 356.6406,22.7656 C356.5,22.6406 356.4688,22.5156 356.4063,22.1094 C356.3125,21.7031 356.1406,21.4844 355.6563,21.2031 C355.1875,20.9531 354.5781,20.7969 354,20.7969 C352.2656,20.7969 351.0156,22.1094 351.0156,23.8906 L351.0156,24.9844 C351.0156,26.6875 352.3125,27.7813 354.3594,27.7813 C355.125,27.7813 355.8125,27.6563 356.2344,27.3906 C356.4219,27.2969 356.4219,27.2969 356.875,26.8125 C357.0625,26.625 357.2656,26.5469 357.4844,26.5469 C357.9531,26.5469 358.3438,26.9375 358.3438,27.3906 C358.3438,27.7813 358.0156,28.2344 357.4375,28.6406 C356.6875,29.1875 355.5313,29.4844 354.3125,29.4844 C351.4219,29.4844 349.3125,27.5938 349.3125,25.0156 L349.3125,23.8906 C349.3125,21.1719 351.3125,19.0938 353.9375,19.0938 C354.8125,19.0938 355.4063,19.2344 356.3438,19.6719 Z ""/><text fill=""#000000"" font-family=""sans-serif"" font-size=""12"" lengthAdjust=""spacingAndGlyphs"" textLength=""34"" x=""368"" y=""28.1543"">Thing</text><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""340"" x2=""404"" y1=""40"" y2=""40""/><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""340"" x2=""404"" y1=""48"" y2=""48""/><!--class Agent-->
            <rect fill=""#FEFECE"" filter=""url(#fufz74as0n034)"" height=""48"" id=""Agent"" style=""stroke: #A80036; stroke-width: 1.5;"" width=""69"" x=""415.5"" y=""195""/><ellipse cx=""430.5"" cy=""211"" fill=""#ADD1B2"" rx=""11"" ry=""11"" style=""stroke: #A80036; stroke-width: 1.0;""/><path d=""M432.8438,206.6719 L433.0156,206.75 C433.2344,206.4375 433.4375,206.3438 433.7344,206.3438 C434.0313,206.3438 434.3125,206.4844 434.4688,206.75 C434.5625,206.9063 434.5781,207.0313 434.5781,207.4688 L434.5781,208.8906 C434.5781,209.3125 434.5469,209.5 434.4375,209.6563 C434.2656,209.875 434,210.0156 433.7344,210.0156 C433.5156,210.0156 433.2813,209.9063 433.1406,209.7656 C433,209.6406 432.9688,209.5156 432.9063,209.1094 C432.8125,208.7031 432.6406,208.4844 432.1563,208.2031 C431.6875,207.9531 431.0781,207.7969 430.5,207.7969 C428.7656,207.7969 427.5156,209.1094 427.5156,210.8906 L427.5156,211.9844 C427.5156,213.6875 428.8125,214.7813 430.8594,214.7813 C431.625,214.7813 432.3125,214.6563 432.7344,214.3906 C432.9219,214.2969 432.9219,214.2969 433.375,213.8125 C433.5625,213.625 433.7656,213.5469 433.9844,213.5469 C434.4531,213.5469 434.8438,213.9375 434.8438,214.3906 C434.8438,214.7813 434.5156,215.2344 433.9375,215.6406 C433.1875,216.1875 432.0313,216.4844 430.8125,216.4844 C427.9219,216.4844 425.8125,214.5938 425.8125,212.0156 L425.8125,210.8906 C425.8125,208.1719 427.8125,206.0938 430.4375,206.0938 C431.3125,206.0938 431.9063,206.2344 432.8438,206.6719 Z ""/><text fill=""#000000"" font-family=""sans-serif"" font-size=""12"" lengthAdjust=""spacingAndGlyphs"" textLength=""37"" x=""444.5"" y=""215.1543"">Agent</text><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""416.5"" x2=""483.5"" y1=""227"" y2=""227""/><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""416.5"" x2=""483.5"" y1=""235"" y2=""235""/>
            <!--class Artist--><rect fill=""#FEFECE"" filter=""url(#fufz74as0n034)"" height=""48"" id=""Artist"" style=""stroke: #A80036; stroke-width: 1.5;"" width=""65"" x=""102.5"" y=""545""/><ellipse cx=""117.5"" cy=""561"" fill=""#ADD1B2"" rx=""11"" ry=""11"" style=""stroke: #A80036; stroke-width: 1.0;""/><path d=""M119.8438,556.6719 L120.0156,556.75 C120.2344,556.4375 120.4375,556.3438 120.7344,556.3438 C121.0313,556.3438 121.3125,556.4844 121.4688,556.75 C121.5625,556.9063 121.5781,557.0313 121.5781,557.4688 L121.5781,558.8906 C121.5781,559.3125 121.5469,559.5 121.4375,559.6563 C121.2656,559.875 121,560.0156 120.7344,560.0156 C120.5156,560.0156 120.2813,559.9063 120.1406,559.7656 C120,559.6406 119.9688,559.5156 119.9063,559.1094 C119.8125,558.7031 119.6406,558.4844 119.1563,558.2031 C118.6875,557.9531 118.0781,557.7969 117.5,557.7969 C115.7656,557.7969 114.5156,559.1094 114.5156,560.8906 L114.5156,561.9844 C114.5156,563.6875 115.8125,564.7813 117.8594,564.7813 C118.625,564.7813 119.3125,564.6563 119.7344,564.3906 C119.9219,564.2969 119.9219,564.2969 120.375,563.8125 C120.5625,563.625 120.7656,563.5469 120.9844,563.5469 C121.4531,563.5469 121.8438,563.9375 121.8438,564.3906 C121.8438,564.7813 121.5156,565.2344 120.9375,565.6406 C120.1875,566.1875 119.0313,566.4844 117.8125,566.4844 C114.9219,566.4844 112.8125,564.5938 112.8125,562.0156 L112.8125,560.8906 C112.8125,558.1719 114.8125,556.0938 117.4375,556.0938 C118.3125,556.0938 118.9063,556.2344 119.8438,556.6719 Z ""/><text fill=""#000000"" font-family=""sans-serif"" font-size=""12"" lengthAdjust=""spacingAndGlyphs"" textLength=""33"" x=""131.5"" y=""565.1543"">Artist</text><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""103.5"" x2=""166.5"" y1=""577"" y2=""577""/><line style=""stroke: #A80036; stroke-width: 1.5;"" x1=""103.5"" x2=""166.5"" y1=""585"" y2=""585""/><!--link Film to Person--><path d=""M254.75,359.205 C254.75,384.224 254.75,414.796 254.75,435.667 "" fill=""none"" id=""Film-Person"" style=""stroke: #A80036; stroke-width: 1.0;""/><text fill=""#000000"" font-family=""sans-serif"" font-size=""13"" lengthAdjust=""spacingAndGlyphs"" textLength=""96"" x=""202"" y=""402.0669"">director sup:80</text><!--link Work to Actor--><path d=""M240.374,141 C176.366,141 37.5,141 37.5,141 C37.5,141 37.5,549.392 37.5,653.883 "" fill=""none"" id=""Work-Actor"" style=""stroke: #A80036; stroke-width: 1.0;""/><text fill=""#000000"" font-family=""sans-serif"" font-size=""13"" lengthAdjust=""spacingAndGlyphs"" textLength=""96"" x=""7"" y=""464.5669"">starring sup:80</text><!--link Work to Person--><path d=""M289.333,165.239 C289.333,224.561 289.333,376.515 289.333,435.798 "" fill=""none"" id=""Work-Person"" style=""stroke: #A80036; stroke-width: 1.0;""/><text fill=""#000000"" font-family=""sans-serif"" font-size=""13"" lengthAdjust=""spacingAndGlyphs"" textLength=""82"" x=""354"" y=""320.5669"">writer sup:62</text><!--link Work to Film--><path d=""M261.5,185.333 C261.5,185.333 261.5,272.678 261.5,272.678 "" fill=""none"" id=""Work-Film"" style=""stroke: #A80036; stroke-width: 1.0;""/><polygon fill=""none"" points=""254.5,185.333,261.5,165.333,268.5,185.333,254.5,185.333"" style=""stroke: #A80036; stroke-width: 1.0;""/><!--link Thing to Work--><path d=""M318.979,32 C318.979,32 272,32 272,32 C272,32 272,84.697 272,116.809 "" fill=""none"" id=""Thing-Work"" style=""stroke: #A80036; stroke-width: 1.0;""/><polygon fill=""none"" points=""318.979,25,338.979,32,318.979,39,318.979,25"" style=""stroke: #A80036; stroke-width: 1.0;""/><!--link Agent to Person--><path d=""M395.477,227 C395.477,227 296.167,227 296.167,227 C296.167,227 296.167,376.411 296.167,435.942 "" fill=""none"" id=""Agent-Person"" style=""stroke: #A80036; stroke-width: 1.0;""/><polygon fill=""none"" points=""395.477,220,415.477,227,395.477,234,395.477,220"" style=""stroke: #A80036; stroke-width: 1.0;""/><!--link Thing to Agent--><path d=""M372,76.141 C372,76.141 372,211 372,211 C372,211 394.339,211 415.107,211 "" fill=""none"" id=""Thing-Agent"" style=""stroke: #A80036; stroke-width: 1.0;""/><polygon fill=""none"" points=""365,76.141,372,56.141,379,76.141,365,76.141"" style=""stroke: #A80036; stroke-width: 1.0;""/><!--link Artist to Actor--><path d=""M82.1191,569 C82.1191,569 70,569 70,569 C70,569 70,621.6971 70,653.8094 "" fill=""none"" id=""Artist-Actor"" style=""stroke: #A80036; stroke-width: 1.0;""/><polygon fill=""none"" points=""82.1192,562,102.1191,569,82.1191,576,82.1192,562"" style=""stroke: #A80036; stroke-width: 1.0;""/><!--link Person to Artist--><path d=""M206.699,460 C206.699,460 135,460 135,460 C135,460 135,512.697 135,544.809 "" fill=""none"" id=""Person-Artist"" style=""stroke: #A80036; stroke-width: 1.0;""/><polygon fill=""none"" points=""206.699,453,226.699,460,206.699,467,206.699,453"" style=""stroke: #A80036; stroke-width: 1.0;""/>
            </g></svg>";
            Properties = new[]
            {
                "type", "director"
            };
            Message = $"Label: {dataset.Label} // Class: {dataset.Class} // Threshold: {dataset.Threshold}";
            ClassName = dataset.Class;
            Threshold = dataset.Threshold;
            DatasetLabel = dataset.Label;
        }
    }
}