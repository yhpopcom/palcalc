﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.UI.Localization
{
    public class DerivedLocalizableText<Key> : ILocalizableText
    {
        private Func<TranslationLocale, Key, string> converter;

        public DerivedLocalizableText(Func<TranslationLocale, Key, string> converter)
        {
            this.converter = converter;

            // TODO - weak event binding
            Translator.LocaleUpdated += Translator_LocaleUpdated;
        }

        private void Translator_LocaleUpdated()
        {
            Locale = Translator.CurrentLocale;
        }

        public ILocalizedText Bind(Key key)
        {
            return new DerivedLocalizedText<Key>(converter, key);
        }
    }
}
