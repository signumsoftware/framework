(function () {
        
    if (!Array.prototype.indexOf) {
        Array.prototype.indexOf = function (searchElement, fromIndex) {
          if ( this === undefined || this === null ) {
            throw new TypeError( '"this" is null or not defined' );
          }

          var length = this.length >>> 0; // Hack to convert object.length to a UInt32

          fromIndex = +fromIndex || 0;

          if (Math.abs(fromIndex) === Infinity) {
            fromIndex = 0;
          }

          if (fromIndex < 0) {
            fromIndex += length;
            if (fromIndex < 0) {
              fromIndex = 0;
            }
          }

          for (;fromIndex < length; fromIndex++) {
            if (this[fromIndex] === searchElement) {
              return fromIndex;
            }
          }

          return -1;
        };
    }

    if (!Array.prototype.lastIndexOf) {
        Array.prototype.lastIndexOf = function(searchElement /*, fromIndex*/) {
        'use strict';

        if (this === void 0 || this === null) {
            throw new TypeError();
        }

        var n, k,
            t = Object(this),
            len = t.length >>> 0;
        if (len === 0) {
            return -1;
        }

        n = len - 1;
        if (arguments.length > 1) {
            n = Number(arguments[1]);
            if (n != n) {
            n = 0;
            }
            else if (n != 0 && n != (1 / 0) && n != -(1 / 0)) {
            n = (n > 0 ? 1 : -1) * Math.floor(Math.abs(n));
            }
        }

        for (k = n >= 0
                ? Math.min(n, len - 1)
                : len - Math.abs(n); k >= 0; k--) {
            if (k in t && t[k] === searchElement) {
            return k;
            }
        }
        return -1;
        };
    }

    if (!Array.prototype.forEach)
    {
        Array.prototype.forEach = function(fun /*, thisArg */)
        {
        "use strict";

        if (this === void 0 || this === null)
            throw new TypeError();

        var t = Object(this);
        var len = t.length >>> 0;
        if (typeof fun !== "function")
            throw new TypeError();

        var thisArg = arguments.length >= 2 ? arguments[1] : void 0;
        for (var i = 0; i < len; i++)
        {
            if (i in t)
            fun.call(thisArg, t[i], i, t);
        }
        };
    }

    if (!Array.prototype.map) {

        alert("Array.prototype.map Not present");
        //Array.prototype.map = function (fun) {
        //    "use strict";

        //    if (this === void 0 || this === null)
        //        throw new TypeError();

        //    var t = Object(this);
        //    var len = t.length >>> 0;
        //    if (typeof fun !== "function")
        //        throw new TypeError();

        //    var res = new Array(len);
        //    var thisArg = arguments.length >= 2 ? arguments[1] : void 0;
        //    for (var i = 0; i < len; i++) {
        //        if (i in t)
        //            res[i] = fun.call(thisArg, t[i], i, t);
        //    }

        //    return res;
        //};
    }

    if (!Array.prototype.every)
    {
        Array.prototype.every = function(fun /*, thisArg */)
        {
        'use strict';

        if (this === void 0 || this === null)
            throw new TypeError();

        var t = Object(this);
        var len = t.length >>> 0;
        if (typeof fun !== 'function')
            throw new TypeError();

        var thisArg = arguments.length >= 2 ? arguments[1] : void 0;
        for (var i = 0; i < len; i++)
        {
            if (i in t && !fun.call(thisArg, t[i], i, t))
            return false;
        }

        return true;
        };
    }

    if (!Array.prototype.filter)
    {
        Array.prototype.filter = function(fun /*, thisArg */)
        {
        "use strict";

        if (this === void 0 || this === null)
            throw new TypeError();

        var t = Object(this);
        var len = t.length >>> 0;
        if (typeof fun !== "function")
            throw new TypeError();

        var res = [];
        var thisArg = arguments.length >= 2 ? arguments[1] : void 0;
        for (var i = 0; i < len; i++)
        {
            if (i in t)
            {
            var val = t[i];

            // NOTE: Technically this should Object.defineProperty at
            //       the next index, as push can be affected by
            //       properties on Object.prototype and Array.prototype.
            //       But that method's new, and collisions should be
            //       rare, so use the more-compatible alternative.
            if (fun.call(thisArg, val, i, t))
                res.push(val);
            }
        }

        return res;
        };
    }

    if (!Array.prototype.some)
    {
        Array.prototype.some = function(fun /*, thisArg */)
        {
        'use strict';

        if (this === void 0 || this === null)
            throw new TypeError();

        var t = Object(this);
        var len = t.length >>> 0;
        if (typeof fun !== 'function')
            throw new TypeError();

        var thisArg = arguments.length >= 2 ? arguments[1] : void 0;
        for (var i = 0; i < len; i++)
        {
            if (i in t && fun.call(thisArg, t[i], i, t))
            return true;
        }

        return false;
        };
    }

    if ( 'function' !== typeof Array.prototype.reduce ) {
        Array.prototype.reduce = function( callback /*, initialValue*/ ) {
        'use strict';
        if ( null === this || 'undefined' === typeof this ) {
            throw new TypeError(
                'Array.prototype.reduce called on null or undefined' );
        }
        if ( 'function' !== typeof callback ) {
            throw new TypeError( callback + ' is not a function' );
        }
        var t = Object( this ), len = t.length >>> 0, k = 0, value;
        if ( arguments.length >= 2 ) {
            value = arguments[1];
        } else {
            while ( k < len && !(k in t)) k++; 
            if ( k >= len )
            throw new TypeError('Reduce of empty array with no initial value');
            value = t[ k++ ];
        }
        for ( ; k < len ; k++ ) {
            if ( k in t ) {
                value = callback( value, t[k], k, t );
            }
        }
        return value;
        };
    }

    if ( 'function' !== typeof Array.prototype.reduceRight ) {
        Array.prototype.reduceRight = function( callback /*, initialValue*/ ) {
        'use strict';
        if ( null === this || 'undefined' === typeof this ) {
            throw new TypeError(
                'Array.prototype.reduce called on null or undefined' );
        }
        if ( 'function' !== typeof callback ) {
            throw new TypeError( callback + ' is not a function' );
        }
        var t = Object( this ), len = t.length >>> 0, k = len - 1, value;
        if ( arguments.length >= 2 ) {
            value = arguments[1];
        } else {
            while ( k >= 0 && !(k in t)) k--;
            if ( k < 0 )
            throw new TypeError('Reduce of empty array with no initial value');
            value = t[ k-- ];
        }
        for ( ; k >= 0 ; k-- ) {
            if ( k in t ) {
                value = callback( value, t[k], k, t );
            }
        }
        return value;
        };
    }
})();