/*window['Autocompleter'] = Autocompleter;
window['Autocompleter.prototype'] = Autocompleter.prototype;
window['AutocompleteOnSelected'] = AutocompleteOnSelected;*/

$(function() { $('#form input[type=text]').keypress(function(e) { return e.which != 13 }) })

/*window['Autocompleter'] = Autocompleter;
window['Autocompleter.prototype'] = Autocompleter.prototype;
window['AutocompleteOnSelected'] = AutocompleteOnSelected;*/
Autocompleter = function(controlId, url, _options) {
    var self = this;
    self.options = $.extend({
        minChars: 1,
        limit: 5,
        delay: 200,
        process: null,
        onSuccess: null,
        entityIdFieldName: null,
        textField: "text",
        extraParams: {},

        extraResult: null,
        extraResultClick: null,

        cacheResults: true
    }, _options);

    self.timerID = 10;
    self.$dd = self.currentText = self.request = undefined;
    self.$control = $("#" + controlId);
    self.controlId = controlId;
    self.url = url;
    self.cacheResults = [];
    self.currentResults = [];
    self.currentInput = undefined;
    self.resultClass = "ddlAuto";
    self.resultSelectedClass = "ddlAutoOn";
    self.create();

    self.extraObj = undefined;  //data object passed to extra div when clicked
};

Autocompleter.prototype = {
    create: function() {
        var self = this;

        this.$control.bind({
            keyup: function(e) {
                self.clear(e.which ? e.which : e.keyCode);
            },
            keydown: function(e) {
                return self.keydown(e);
            },
            click: function() {
                return false;
            },
            focusin: function() {
                if (self.currentResults.length) {
                    self.$dd.show();
                }
            }
        });

        $("body").click(function() {
            self.$dd.hide();
        });

        this.$dd = $("<div/>").addClass("AutoCompleteMainDiv");
        this.$dd.click(function(e) {
            self.click(e);
        });

        this.$dd.insertAfter(this.$control);
        this.$dd.delegate("." + this.resultClass, "mouseenter", function() {
            self.selectIndex($(this));
        });


    },
    clear: function(e) {
        clearTimeout(this.timerID);
        var self = this;
        this.timerID = setTimeout(function() { self.keyup(e) }, (self.options.cacheResults && self.cacheResults[self.$control.val().toLowerCase()] != null) ? 0 : self.options.delay);
    },
    keyup: function(key) {
        if (key == 37 || key == 39 || key == 38 || key == 40 || key == 13) return;
        var input = this.$control.val();
        if (this.currentText == input) return;

        if (input != null && input.length < this.options.minChars) {
            this.$dd.html("").hide(); this.currentResults = [];
            return;
        }
        //if (this.currentResults.length < this.options.limit && input.indexOf(this.currentInput) != -1)
        //process cached results

        var data = $.extend({
            q: input, l: this.options.limit
        }, this.options.extraParams);

        var self = this;

        if (self.currentResults == [] && self.currentInput != null && input.indexOf(self.currentInput) == 0) {
            self.showResults([], input);
            return;
        }

        if (self.options.cacheResults && self.cacheResults[input.toLowerCase()] != null) {
            self.showResults(self.cacheResults[input.toLowerCase()], input);
            return;
        }

        if (self.request) self.request.abort();
        self.$control.addClass('loading');
        self.request = $.getJSON(
            self.url, data,
            function(results) {
                self.request = undefined;
                if (results) {
                    self.showResults(results, input);
                }
            });
    },

    showResults: function(results, input) {
        var self = this;
        self.currentInput = input;
        self.currentText = self.$control.val();

        var prevCount = self.currentResults.length;
        if (prevCount == 0) self.$dd.hide();

        var content = "";
        for (var i = 0, l = results.length; i < l; i++) {
            content += "<div class=\"" + self.resultClass + "\">" + self.process(input, results[i]) + "</div>";
        }

        self.$dd[0].innerHTML = content;

        self.currentResults = results;
        if (self.options.cacheResults)
            self.cacheResults[input.toLowerCase()] = results;

        //add extra result
        if (self.options.extraResult != null) {
            var obj = {};
            obj.input = input;
            obj.results = results.length;

            var extraDiv = "<div class=\"" + self.resultClass + " " + " extra\">" + self.options.extraResult(obj) + "</div>";
            self.extraObj = obj;
            
            self.$dd[0].innerHTML += extraDiv;
        }

        var offset = self.$control.position();
        self.$dd.css({
            left: offset.left,
            top: offset.top + self.$control.outerHeight() - 1,
            width: self.$control.outerWidth() - 2
        });

        self.$control.removeClass('loading');

        if (prevCount == 0)
            self.$dd.slideDown("fast");
        else
            self.$dd.show();
    },
    
    keydown: function(e) {
        var key = e.which ? e.which : e.keyCode;
        if (key == 13 || key == 9) {    //enter or tab
            var selectedOption = this.$dd.find("." + this.resultSelectedClass);
            if (selectedOption.length > 0) {
                if (selectedOption.hasClass("extra")) selectedOption.click();
                else this.onOk(selectedOption.index());
            }
            if (key == 9) {
                this.$dd.hide();
            }
            if (key == 13) {    //no bubble for enter
                return false;
            }
            return;
        }
        if (key == 38) { //Arrow up
            if (this.currentText != "") { //autocomplete dropdown is shown
                this.moveUp();
                return;
            }
        }
        if (key == 40) { //Arrow down
            if (this.currentText != "") { //autocomplete dropdown is shown
                this.moveDown();
                return;
            }
        }
    },
    moveUp: function() {
        var current = this.$dd.children("." + this.resultSelectedClass).first();
        if (!current.length) { //Not yet in the DDL, select the last one		
            this.selectIndex(this.$dd.children().last());
            return;
        }
        this.selectIndex(current.prev());
    },
    moveDown: function() {
        var current = this.$dd.children("." + this.resultSelectedClass).first();
        if (!current.length) { //Not yet in the DDL, select the first one
            this.selectIndex(this.$dd.children());
            return;
        }
        this.selectIndex(current.next());
    },
    click: function(e) {
        var target = e.srcElement || e.target;
        if (target != null) {
            if (!$(target).closest(".extra").length)
                this.onOk($(target).closest("." + this.resultSelectedClass).index());
            else
                if (this.options.extraResultClick != null)
                this.options.extraResultClick(this.extraObj);
            this.$dd.hide();
        }
    },
    process: function(i, s) {
        if (this.options.process != null) return this.options.process(i, s);
        return this.highlight(i, s[this.options.textField]);
    },
    highlight: function(i, s) {
        var pre_s = s;
        s = s.replace(new RegExp("(" + i + ")", "gi"), '<strong>$1</strong>');

        if (pre_s == s) {
            //look if there are non-bolded strings
            var nd_i = replaceDiacritics(i).toLowerCase();
            var nd_s = replaceDiacritics(s).toLowerCase();

            var index = nd_s.indexOf(nd_i), l = i.length;
            if (index != -1) s = s.substr(0, index) + "<strong>" + s.substr(index, i.length) + "</strong>" + s.substr(index + i.length);
        }
        return s;
    },
    onOk: function(index) {
        this.$dd.hide();

        var data = this.currentResults[index];
        this.$control.val(data[this.options.textField]);

        if (this.options.onSuccess != null) {
            this.options.onSuccess(this.$control, data);
            return;
        }
        var id = data.id;
        if (this.options.entityIdFieldName != null) {
            $('#' + this.options.entityIdFieldName).val(id);
            AutocompleteOnSelected(this.controlId, data);
        }
    },
    selectIndex: function($option) {
        this.$dd.children("." + this.resultSelectedClass).removeClass(this.resultSelectedClass);
        if ($option == null || $option == undefined) {
            this.$control.val(this.currentText).focus();
            return;
        }
        $option.first().addClass(this.resultSelectedClass);
        this.$control.focus();
    }
};

function replaceDiacritics(s) {
    var diacritics = [
        /[\300-\306]/g, /[\340-\346]/g,  // A, a
        /[\310-\313]/g, /[\350-\353]/g,  // E, e
        /[\314-\317]/g, /[\354-\357]/g,  // I, i
        /[\322-\330]/g, /[\362-\370]/g,  // O, o
        /[\331-\334]/g, /[\371-\374]/g,  // U, u
        /[\321]/g, /[\361]/g, // N, n
        /[\307]/g, /[\347]/g // C, c
    ];
    var chars = ['A', 'a', 'E', 'e', 'I', 'i', 'O', 'o', 'U', 'u', 'N', 'n', 'C', 'c'];

    for (var i = 0; i < diacritics.length; i++) {
        s = s.replace(diacritics[i], chars[i]);
    }
    return (s);
}
