/*window['Autocompleter'] = Autocompleter;
window['Autocompleter.prototype'] = Autocompleter.prototype;
window['AutocompleteOnSelected'] = AutocompleteOnSelected;*/

$(function() { $('#form input[type=text]').keypress(function(e) { return e.which != 13 }) })

/*window['Autocompleter'] = Autocompleter;
window['Autocompleter.prototype'] = Autocompleter.prototype;
window['AutocompleteOnSelected'] = AutocompleteOnSelected;*/
Autocompleter = function(controlId, url, _options) {
    this.options = $.extend({
        minChars: 1,
        limit: 5,
        delay: 200,
        process: null,
        onSuccess: null,
        entityIdFieldName: null,
        textField: "text",
        extraParams: {},
        cacheEnabled: true,
        showExtra: false,
        renderExtra: function($extra, data) {
            return $extra;
        }
    }, _options);

    this.timerID = undefined;
    this.$dd = this.prevInput = this.request = undefined;
    this.$control = $("#" + controlId);
    this.controlId = controlId;
    this.url = url;
    this.cache = this.currentResults = [];
    this.resultClass = "ddlAuto";
    this.resultSelectedClass = "ddlAutoOn";
    this.create();

    this.extraObj = undefined;  //data object passed to extra div when clicked
 
};

Autocompleter.prototype = {
    create: function() {
        var self = this;

        this.extraObj = undefined;  //data object passed to extra div when clicked
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

        this.$dd = $("<div/>")
            .addClass("AutoCompleteMainDiv")
            .click(function(e) { self.click(e); })
            .delegate("." + this.resultClass, "mouseenter", function() {
                self.selectIndex($(this))
            })
            .insertAfter(this.$control);
    },
    clear: function(e) {
        clearTimeout(this.timerID);
        var self = this;
        this.timerID = setTimeout(function() { self.keyup(e) }, (self.options.cacheEnabled && self.$control.val().toLowerCase() in self.cache) ? 0 : self.options.delay);
    },
    keyup: function(key) {
        if (key == 37 || key == 39 || key == 38 || key == 40 || key == 13) return;
        var input = this.$control.val();
        if (this.prevInput == input) return;

        this.prevInput = input;

        if (input != null && input.length < this.options.minChars) {
            this.$dd.html("").hide(); this.currentResults = [];
            return;
        }
        //if (this.currentResults.length < this.options.limit && input.indexOf(this.currentInput) != -1)
        //process cached results

        var data = $.extend({
            q: input, l: this.options.limit
        }, this.options.extraParams);

        if (this.options.cacheEnabled && input.toLowerCase() in this.cache) {
            this.showResults(this.cache[input.toLowerCase()], input);
            return;
        }

        var self = this;

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
        var prevCount = this.currentResults.length;
        if (prevCount == 0) this.$dd.hide();

        var content = [];
        for (var i = 0, l = results.length; i < l; i++) {
            content.push("<div class=\"" + this.resultClass + "\">" + this.process(input, results[i]) + "</div>");
        }

        this.$dd[0].innerHTML = content.join('');

        this.currentResults = results;
        if (this.options.cacheEnabled)
            this.cache[input.toLowerCase()] = results;

        //add extra result
        if (this.options.showExtra) {
            var obj = {
                input: input,
                results: results.length
            };

            var $extra = $("<div/>")
                            .addClass(this.resultClass + " extra");

            this.options.renderExtra($extra, obj);
            this.$dd.append($extra);
        }

        var offset = this.$control.position();
        this.$dd.css({
            left: offset.left,
            top: offset.top + this.$control.outerHeight() - 1,
            width: this.$control.outerWidth() - 2
        });

        this.$control.removeClass('loading');

        if (prevCount == 0 && !this.options.showExtra)
            this.$dd.slideDown("fast");
        else
            this.$dd.show();
    },

    keydown: function(e) {
        var key = e.which ? e.which : e.keyCode;
        if (key == 13 || key == 9) {    //enter or tab
            var selectedOption = this._getSelected();
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
        var current = this._getSelected();
        if (!current.length) { //Not yet in the DDL, select the last one		
            this.selectIndex(this.$dd.children().last());
            return;
        }
        this.selectIndex(current.prev());
    },
    moveDown: function() {
        var current = this._getSelected();
        if (!current.length) { //Not yet in the DDL, select the first one
            this.selectIndex(this.$dd.children());
            return;
        }
        this.selectIndex(current.next());
    },
    click: function(e) {
        var target = e.srcElement || e.target;
        if (target != null) {
            var $extra = $(target).closest(".extra");

            if (!$extra.length)
                this.onOk($(target).closest("." + this.resultSelectedClass).index());
            else
                $extra.click();
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
        this._getSelected().removeClass(this.resultSelectedClass);
        if ($option == null || $option == undefined) {
            this.$control.val(this.currentText).focus();
            return;
        }
        $option.first().addClass(this.resultSelectedClass);
        this.$control.focus();
    },

    _getSelected: function() {
        return this.$dd.children("." + this.resultSelectedClass).first();
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
