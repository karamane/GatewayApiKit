import {
  __commonJS
} from "./chunk-4MBMRILA.js";

// node_modules/@babel/runtime/helpers/interopRequireDefault.js
var require_interopRequireDefault = __commonJS({
  "node_modules/@babel/runtime/helpers/interopRequireDefault.js"(exports, module) {
    function _interopRequireDefault(e) {
      return e && e.__esModule ? e : {
        "default": e
      };
    }
    module.exports = _interopRequireDefault, module.exports.__esModule = true, module.exports["default"] = module.exports;
  }
});

// node_modules/rc-pagination/lib/locale/tr_TR.js
var require_tr_TR = __commonJS({
  "node_modules/rc-pagination/lib/locale/tr_TR.js"(exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", {
      value: true
    });
    exports.default = void 0;
    var locale = {
      // Options
      items_per_page: "/ sayfa",
      jump_to: "Git",
      jump_to_confirm: "onayla",
      page: "Sayfa",
      // Pagination
      prev_page: "Önceki Sayfa",
      next_page: "Sonraki Sayfa",
      prev_5: "Önceki 5 Sayfa",
      next_5: "Sonraki 5 Sayfa",
      prev_3: "Önceki 3 Sayfa",
      next_3: "Sonraki 3 Sayfa",
      page_size: "sayfa boyutu"
    };
    var _default = exports.default = locale;
  }
});

// node_modules/@babel/runtime/helpers/typeof.js
var require_typeof = __commonJS({
  "node_modules/@babel/runtime/helpers/typeof.js"(exports, module) {
    function _typeof(o) {
      "@babel/helpers - typeof";
      return module.exports = _typeof = "function" == typeof Symbol && "symbol" == typeof Symbol.iterator ? function(o2) {
        return typeof o2;
      } : function(o2) {
        return o2 && "function" == typeof Symbol && o2.constructor === Symbol && o2 !== Symbol.prototype ? "symbol" : typeof o2;
      }, module.exports.__esModule = true, module.exports["default"] = module.exports, _typeof(o);
    }
    module.exports = _typeof, module.exports.__esModule = true, module.exports["default"] = module.exports;
  }
});

// node_modules/@babel/runtime/helpers/toPrimitive.js
var require_toPrimitive = __commonJS({
  "node_modules/@babel/runtime/helpers/toPrimitive.js"(exports, module) {
    var _typeof = require_typeof()["default"];
    function toPrimitive(t, r) {
      if ("object" != _typeof(t) || !t) return t;
      var e = t[Symbol.toPrimitive];
      if (void 0 !== e) {
        var i = e.call(t, r || "default");
        if ("object" != _typeof(i)) return i;
        throw new TypeError("@@toPrimitive must return a primitive value.");
      }
      return ("string" === r ? String : Number)(t);
    }
    module.exports = toPrimitive, module.exports.__esModule = true, module.exports["default"] = module.exports;
  }
});

// node_modules/@babel/runtime/helpers/toPropertyKey.js
var require_toPropertyKey = __commonJS({
  "node_modules/@babel/runtime/helpers/toPropertyKey.js"(exports, module) {
    var _typeof = require_typeof()["default"];
    var toPrimitive = require_toPrimitive();
    function toPropertyKey(t) {
      var i = toPrimitive(t, "string");
      return "symbol" == _typeof(i) ? i : i + "";
    }
    module.exports = toPropertyKey, module.exports.__esModule = true, module.exports["default"] = module.exports;
  }
});

// node_modules/@babel/runtime/helpers/defineProperty.js
var require_defineProperty = __commonJS({
  "node_modules/@babel/runtime/helpers/defineProperty.js"(exports, module) {
    var toPropertyKey = require_toPropertyKey();
    function _defineProperty(e, r, t) {
      return (r = toPropertyKey(r)) in e ? Object.defineProperty(e, r, {
        value: t,
        enumerable: true,
        configurable: true,
        writable: true
      }) : e[r] = t, e;
    }
    module.exports = _defineProperty, module.exports.__esModule = true, module.exports["default"] = module.exports;
  }
});

// node_modules/@babel/runtime/helpers/objectSpread2.js
var require_objectSpread2 = __commonJS({
  "node_modules/@babel/runtime/helpers/objectSpread2.js"(exports, module) {
    var defineProperty = require_defineProperty();
    function ownKeys(e, r) {
      var t = Object.keys(e);
      if (Object.getOwnPropertySymbols) {
        var o = Object.getOwnPropertySymbols(e);
        r && (o = o.filter(function(r2) {
          return Object.getOwnPropertyDescriptor(e, r2).enumerable;
        })), t.push.apply(t, o);
      }
      return t;
    }
    function _objectSpread2(e) {
      for (var r = 1; r < arguments.length; r++) {
        var t = null != arguments[r] ? arguments[r] : {};
        r % 2 ? ownKeys(Object(t), true).forEach(function(r2) {
          defineProperty(e, r2, t[r2]);
        }) : Object.getOwnPropertyDescriptors ? Object.defineProperties(e, Object.getOwnPropertyDescriptors(t)) : ownKeys(Object(t)).forEach(function(r2) {
          Object.defineProperty(e, r2, Object.getOwnPropertyDescriptor(t, r2));
        });
      }
      return e;
    }
    module.exports = _objectSpread2, module.exports.__esModule = true, module.exports["default"] = module.exports;
  }
});

// node_modules/rc-picker/lib/locale/common.js
var require_common = __commonJS({
  "node_modules/rc-picker/lib/locale/common.js"(exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", {
      value: true
    });
    exports.commonLocale = void 0;
    var commonLocale = exports.commonLocale = {
      yearFormat: "YYYY",
      dayFormat: "D",
      cellMeridiemFormat: "A",
      monthBeforeYear: true
    };
  }
});

// node_modules/rc-picker/lib/locale/tr_TR.js
var require_tr_TR2 = __commonJS({
  "node_modules/rc-picker/lib/locale/tr_TR.js"(exports) {
    "use strict";
    var _interopRequireDefault = require_interopRequireDefault().default;
    Object.defineProperty(exports, "__esModule", {
      value: true
    });
    exports.default = void 0;
    var _objectSpread2 = _interopRequireDefault(require_objectSpread2());
    var _common = require_common();
    var locale = (0, _objectSpread2.default)((0, _objectSpread2.default)({}, _common.commonLocale), {}, {
      locale: "tr_TR",
      today: "Bugün",
      now: "Şimdi",
      backToToday: "Bugüne Geri Dön",
      ok: "Tamam",
      clear: "Temizle",
      week: "Hafta",
      month: "Ay",
      year: "Yıl",
      timeSelect: "Zaman Seç",
      dateSelect: "Tarih Seç",
      monthSelect: "Ay Seç",
      yearSelect: "Yıl Seç",
      decadeSelect: "On Yıl Seç",
      dateFormat: "DD/MM/YYYY",
      dateTimeFormat: "DD/MM/YYYY HH:mm:ss",
      previousMonth: "Önceki Ay (PageUp)",
      nextMonth: "Sonraki Ay (PageDown)",
      previousYear: "Önceki Yıl (Control + Sol)",
      nextYear: "Sonraki Yıl (Control + Sağ)",
      previousDecade: "Önceki On Yıl",
      nextDecade: "Sonraki On Yıl",
      previousCentury: "Önceki Yüzyıl",
      nextCentury: "Sonraki Yüzyıl",
      shortWeekDays: ["Paz", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt"],
      shortMonths: ["Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"]
    });
    var _default = exports.default = locale;
  }
});

// node_modules/antd/lib/time-picker/locale/tr_TR.js
var require_tr_TR3 = __commonJS({
  "node_modules/antd/lib/time-picker/locale/tr_TR.js"(exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", {
      value: true
    });
    exports.default = void 0;
    var locale = {
      placeholder: "Zaman seç",
      rangePlaceholder: ["Başlangıç zamanı", "Bitiş zamanı"]
    };
    var _default = exports.default = locale;
  }
});

// node_modules/antd/lib/date-picker/locale/tr_TR.js
var require_tr_TR4 = __commonJS({
  "node_modules/antd/lib/date-picker/locale/tr_TR.js"(exports) {
    "use strict";
    var _interopRequireDefault = require_interopRequireDefault().default;
    Object.defineProperty(exports, "__esModule", {
      value: true
    });
    exports.default = void 0;
    var _tr_TR = _interopRequireDefault(require_tr_TR2());
    var _tr_TR2 = _interopRequireDefault(require_tr_TR3());
    var locale = {
      lang: Object.assign({
        placeholder: "Tarih seç",
        yearPlaceholder: "Yıl seç",
        quarterPlaceholder: "Çeyrek seç",
        monthPlaceholder: "Ay seç",
        weekPlaceholder: "Hafta seç",
        rangePlaceholder: ["Başlangıç tarihi", "Bitiş tarihi"],
        rangeYearPlaceholder: ["Başlangıç yılı", "Bitiş yılı"],
        rangeMonthPlaceholder: ["Başlangıç ayı", "Bitiş ayı"],
        rangeWeekPlaceholder: ["Başlangıç haftası", "Bitiş haftası"]
      }, _tr_TR.default),
      timePickerLocale: Object.assign({}, _tr_TR2.default)
    };
    var _default = exports.default = locale;
  }
});

// node_modules/antd/lib/calendar/locale/tr_TR.js
var require_tr_TR5 = __commonJS({
  "node_modules/antd/lib/calendar/locale/tr_TR.js"(exports) {
    "use strict";
    var _interopRequireDefault = require_interopRequireDefault().default;
    Object.defineProperty(exports, "__esModule", {
      value: true
    });
    exports.default = void 0;
    var _tr_TR = _interopRequireDefault(require_tr_TR4());
    var _default = exports.default = _tr_TR.default;
  }
});

// node_modules/antd/lib/locale/tr_TR.js
var require_tr_TR6 = __commonJS({
  "node_modules/antd/lib/locale/tr_TR.js"(exports) {
    "use strict";
    var _interopRequireDefault = require_interopRequireDefault().default;
    Object.defineProperty(exports, "__esModule", {
      value: true
    });
    exports.default = void 0;
    var _tr_TR = _interopRequireDefault(require_tr_TR());
    var _tr_TR2 = _interopRequireDefault(require_tr_TR5());
    var _tr_TR3 = _interopRequireDefault(require_tr_TR4());
    var _tr_TR4 = _interopRequireDefault(require_tr_TR3());
    var typeTemplate = "${label} geçerli bir ${type} değil";
    var localeValues = {
      locale: "tr",
      Pagination: _tr_TR.default,
      DatePicker: _tr_TR3.default,
      TimePicker: _tr_TR4.default,
      Calendar: _tr_TR2.default,
      global: {
        placeholder: "Lütfen seçiniz",
        close: "Kapat"
      },
      Table: {
        filterTitle: "Filtre menüsü",
        filterConfirm: "Tamam",
        filterReset: "Sıfırla",
        filterEmptyText: "Filtre yok",
        filterCheckAll: "Tümünü seç",
        selectAll: "Tüm sayfayı seç",
        selectInvert: "Tersini seç",
        selectionAll: "Tümünü seç",
        sortTitle: "Sırala",
        expand: "Satırı genişlet",
        collapse: "Satırı daralt",
        triggerDesc: "Azalan düzende sırala",
        triggerAsc: "Artan düzende sırala",
        cancelSort: "Sıralamayı kaldır"
      },
      Tour: {
        Next: "Sonraki",
        Previous: "Önceki",
        Finish: "Bitir"
      },
      Modal: {
        okText: "Tamam",
        cancelText: "İptal",
        justOkText: "Tamam"
      },
      Popconfirm: {
        okText: "Tamam",
        cancelText: "İptal"
      },
      Transfer: {
        titles: ["", ""],
        searchPlaceholder: "Arama",
        itemUnit: "Öğe",
        itemsUnit: "Öğeler",
        remove: "Kaldır",
        selectCurrent: "Tüm sayfayı seç",
        removeCurrent: "Sayfayı kaldır",
        selectAll: "Tümünü seç",
        deselectAll: "Tümünün seçimini kaldır",
        removeAll: "Tümünü kaldır",
        selectInvert: "Tersini seç"
      },
      Upload: {
        uploading: "Yükleniyor...",
        removeFile: "Dosyayı kaldır",
        uploadError: "Yükleme hatası",
        previewFile: "Dosyayı önizle",
        downloadFile: "Dosyayı indir"
      },
      Empty: {
        description: "Veri Yok"
      },
      Icon: {
        icon: "ikon"
      },
      Text: {
        edit: "Düzenle",
        copy: "Kopyala",
        copied: "Kopyalandı",
        expand: "Genişlet",
        collapse: "Daralt"
      },
      Form: {
        optional: "(opsiyonel)",
        defaultValidateMessages: {
          default: "Alan doğrulama hatası ${label}",
          required: "${label} gerekli bir alan",
          enum: "${label} şunlardan biri olmalı: [${enum}]",
          whitespace: "${label} sadece boşluk olamaz",
          date: {
            format: "${label} tarih biçimi geçersiz",
            parse: "${label} bir tarihe dönüştürülemedi",
            invalid: "${label} geçersiz bir tarih"
          },
          types: {
            string: typeTemplate,
            method: typeTemplate,
            array: typeTemplate,
            object: typeTemplate,
            number: typeTemplate,
            date: typeTemplate,
            boolean: typeTemplate,
            integer: typeTemplate,
            float: typeTemplate,
            regexp: typeTemplate,
            email: typeTemplate,
            url: typeTemplate,
            hex: typeTemplate
          },
          string: {
            len: "${label} ${len} karakter olmalı",
            min: "${label} en az ${min} karakter olmalı",
            max: "${label} en çok ${max} karakter olmalı",
            range: "${label} ${min}-${max} karakter arası olmalı"
          },
          number: {
            len: "${label} ${len} olmalı",
            min: "${label} en az ${min} olmalı",
            max: "${label} en çok ${max} olmalı",
            range: "${label} ${min}-${max} arası olmalı"
          },
          array: {
            len: "${label} sayısı ${len} olmalı",
            min: "${label} sayısı en az ${min} olmalı",
            max: "${label} sayısı en çok ${max} olmalı",
            range: "${label} sayısı ${min}-${max} arası olmalı"
          },
          pattern: {
            mismatch: "${label} şu kalıpla eşleşmeli: ${pattern}"
          }
        }
      },
      Image: {
        preview: "Önizleme"
      }
    };
    var _default = exports.default = localeValues;
  }
});

// node_modules/antd/locale/tr_TR.js
var require_tr_TR7 = __commonJS({
  "node_modules/antd/locale/tr_TR.js"(exports, module) {
    module.exports = require_tr_TR6();
  }
});
export default require_tr_TR7();
//# sourceMappingURL=antd_locale_tr_TR.js.map
