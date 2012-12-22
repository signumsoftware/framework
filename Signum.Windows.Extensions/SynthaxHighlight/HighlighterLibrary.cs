using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Diagnostics;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Collections;
using System.Text.RegularExpressions;

namespace Signum.Windows.SyntaxHighlight
{
    public class HighlighterLibrary
    {
        public static Highlighter Javascript()
        {
            return new Highlighter
            {
                Rules = 
                {
                    new WordRule(@"break export return case for switch comment function this continue if typeof default import var
delete in void do label while else new with abstract implements protected boolean instanceOf public byte int short char interface 
static double long synchronized false native throws final null transient float package true goto private catch enum throw class 
extends try const finally debugger super")
                    {
                        Formatter = new RuleFormatter("#0080FF")
                    }, 

                    new WordRule(@"alert isFinite personalbar Anchor isNan Plugin Area java print arguments JavaArray prompt Array JavaClass 
prototype assign JavaObject Radio blur JavaPackage ref Boolean length RegExp Button Link releaseEvents callee location Reset caller Location 
resizeBy captureEvents locationbar resizeTo Checkbox Math routeEvent clearInterval menubar scroll clearTimeout MimeType scrollbars close 
moveBy scrollBy closed moveTo scrollTo confirm name Select constructor NaN self Date navigate setInterval defaultStatus navigator setTimeout 
document Navigator status Document netscape statusbar Element Number stop escape Object String eval onBlur Submit FileUpload onError sun find 
onFocus taint focus onLoad Text Form onUnload Textarea Frame open toolbar Frames opener top Function Option toString getClass outerHeight 
unescape Hidden OuterWidth untaint history Packages unwatch History pageXoffset valueOf home pageYoffset watch Image parent window Infinity 
parseFloat Window InnerHeight parseInt InnerWidth Password") 
                    {
                        Formatter = new RuleFormatter("#C00080")
                    }, 

                    new RegexRule
                    {
                        Regex = new Regex(@"(""[^""]*"")|('[^']*')"),
                        Formatter = new RuleFormatter("#A31515")
                    },
                }
            }; 

        }
    }
}