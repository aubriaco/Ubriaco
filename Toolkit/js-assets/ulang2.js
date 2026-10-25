/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

/*
    Current Lang:
        <span class="lang-current"></span>

    For English:
        class="lang-select" lang-ident="en"

    For Spanish:
        class="lang-select" lang-ident="es"
*/

const Languages = [
    { ident: 'en', name: 'English', flag: 'us' },
    { ident: 'es', name: 'Español', flag: 'pr' }
];
var Lang = {};
var CurrentLangItem = Languages[0];
var CurrentLang = CurrentLangItem.ident;
var CurrentLangName = CurrentLangItem.name;
var Lang_DefaultIdent = 'en';


function lang_qget(loc, successCallback, errorCallback, local) {
    fetch(loc, {
        method: 'GET',
        mode: 'cors',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' }
    }).then(async (r) => {
        try {
            var j = await r.json();
            if (successCallback)
                successCallback(j);
        }
        catch (e) {
            if (errorCallback)
                errorCallback(e);
        }
    }, (e) => {
        if (errorCallback)
            errorCallback(e);
    });
}

function lang_qpost(loc, data, successCallback, errorCallback) {
    fetch(loc, {
        method: 'POST',
        mode: 'cors',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    }).then(async (r) => {
        try {
            var j = await r.json();
            if (successCallback)
                successCallback(j);
        }
        catch (e) {
            if (errorCallback)
                errorCallback(e);
        }
    }, (e) => {
        if (errorCallback)
            errorCallback(e);
    });
}

function setCurrentLang(a, c, setqs) {
    CurrentLangItem = a;
    CurrentLang = a.ident;
    CurrentLangName = a.name;
    if (c) {
        lang_qpost('/api/content/lang/set/' + a.ident, (r) => {
            if (r.errorCode)
                console.log('Lang set error', r.errorMessage);
        });
    }

    if (setqs) {
        const url = new URL(window.location);
        url.searchParams.set('lang', CurrentLang);
        window.history.replaceState({}, '', url);
    }
};

function setCurrentLangByIdent(a, c, setqs) {
    Languages.forEach(i => {
        if (i.ident === a) {
            setCurrentLang(i, c, setqs);
        }
    });
};

function loadLang(callback) {
    lang_qget('/lang/dict.json', (r) => {
        Lang = r;
        if (callback)
            callback();
    }, () => { }, true);
}

function getCurrentLangItem() {
    return CurrentLangItem;
}

function getLang(a) {
    a = a.replace(/\n/g, ' ', /\r/, ' ').trim();
    while (a.indexOf('  ') >= 0) a = a.replace('  ', ' ');
    if (Lang && Lang[a] && Lang[a][CurrentLang]) {
        return Lang[a][CurrentLang];
    }
    return a;
}

function getElementsWithText(root = document.body) {
    return [root, ...root.querySelectorAll('*')].filter(element => {
        if (element.matches('script, style, noscript, template')) {
            return false;
        }

        return Array.from(element.childNodes).some(node =>
            node.nodeType === Node.TEXT_NODE &&
            node.textContent.trim() !== ''
        );
    });
}

function scan_lang() {
    let eles = getElementsWithText();
    for (const i of eles) {
        if (!i.getAttribute('lang-data')) {
            i.setAttribute('lang-data', i.innerText);
            i.setAttribute('lang-html', i.innerHTML);
        }
    }
}

function lang_filter_text(a) {
    a = a.replace(/\n/g, ' ', /\r/, ' ').trim();
    while (a.indexOf('  ') >= 0) a = a.replace('  ', ' ');
    return a;
}

function lang_loop() {

    const eles = getElementsWithText();

    for (const i of eles) {
        const langData = i.getAttribute('lang-data');
        const langHTML = i.getAttribute('lang-html');
        const lang = getLang(langData);
        if (lang) {
            const ltext = lang_filter_text(lang);
            const itext = lang_filter_text(i.innerText);
            if (itext != ltext) {
                i.innerHTML = langHTML.replace(langData, lang);
            }
        }
    }


    const selectors = document.getElementsByClassName('lang-select');

    for (const i of selectors) {
        if (!i._lang_listener) {
            i._lang_listener = true;
            i.addEventListener('click', () => {
                const ident = i.getAttribute('lang-ident');
                setCurrentLangByIdent(ident, null, true);
            });
        }
    }

    const currentLang = getCurrentLangItem();

    if (currentLang) {

        const currents = document.getElementsByClassName('lang-current');

        for (const i of currents) {
            i.innerText = currentLang.name;
        }
    }
}

function lang_get_by_loc() {
    const pname = document.location.href;
    let n = pname.indexOf('?');
    if (n >= 0) {
        let qs = pname.substring(n + 1);
        n = qs.indexOf('#');
        if (n >= 0) qs = qs.substring(0, n);
        n = qs.indexOf('lang=');
        if (n >= 0) {
            qs = qs.substring(n + 5);
            n = qs.indexOf('&');
            if (n >= 0)
                return qs.substring(0, n);
            return qs;
        }
    }
    return Lang_DefaultIdent;
}

document.addEventListener('DOMContentLoaded', function () {
    loadLang(() => {
        scan_lang();
        const ident = lang_get_by_loc();
        if (ident)
            setCurrentLangByIdent(ident);
        setInterval(lang_loop, 500);
    });
});