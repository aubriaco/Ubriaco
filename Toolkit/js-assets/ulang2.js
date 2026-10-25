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

function lang_setCookie(name, value, days = 30) {
    const expires = new Date(Date.now() + days * 86400000).toUTCString();
    const secure = location.protocol === 'https:' ? '; Secure' : '';

    document.cookie =
        `${encodeURIComponent(name)}=${encodeURIComponent(value)}` +
        `; Expires=${expires}; Path=/; SameSite=Lax${secure}`;
}

function lang_getCookie(name) {
    const prefix = `${encodeURIComponent(name)}=`;
    const cookie = document.cookie
        .split(';')
        .map(item => item.trim())
        .find(item => item.startsWith(prefix));

    return cookie ? decodeURIComponent(cookie.slice(prefix.length)) : null;
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

    lang_setCookie('lang_ident', a.ident, 365);

    if (setqs) {
        const url = new URL(window.location);
        url.searchParams.set('lang', CurrentLang);
        window.history.replaceState({}, '', url);
    }
};

function setCurrentLangByIdent(a, c, setqs, selectors) {
    Languages.forEach(i => {
        if (i.ident === a) {
            setCurrentLang(i, c, setqs);
        }
    });

    try {
        if (!selectors)
            selectors = document.getElementsByClassName('lang-select');
        Array.from(selectors).forEach(i => {
            if (i.getAttribute('lang-ident') === a && !i.classList.contains('active'))
                i.classList.add('active');
            else if (i.classList.contains('active'))
                i.classList.remove('active');
        });
    }
    catch (e) {
        console.error(e);
    }

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

function lang_filter_text(a) {
    a = a.replace(/\n/g, ' ', /\r/, ' ').trim();
    while (a.indexOf('  ') >= 0) a = a.replace('  ', ' ');
    return a;
}


function getLang(a) {
    if (!a) return '';
    a = lang_filter_text(a);
    if (Lang && Lang[a] && Lang[a][CurrentLang]) {
        return Lang[a][CurrentLang];
    }
    return a;
}

function lang_countLeadingSpaces(str) {
    return str.length - str.trimStart().length;
}

function lang_countTrailingSpaces(str) {
    return str.length - str.trimEnd().length;
}

function lang_getElementsWithText(root = document.body) {
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
    let eles = lang_getElementsWithText();
    for (const i of eles) {
        if (!i.getAttribute('lang-data'))
            i.setAttribute('lang-data', i.textContent);
        if (!i.getAttribute('lang-html'))
            i.setAttribute('lang-html', i.innerHTML);
    }
}


function lang_loop() {

    const eles = lang_getElementsWithText();

    for (const i of eles) {
        const langData = i.getAttribute('lang-data');
        const langHTML = i.getAttribute('lang-html');


        if (langHTML) {
            try {
                let p = [langHTML];
                if (langHTML.indexOf('<') >= 0) {
                    p = [...langHTML.matchAll(/(?:^|>)([^<]+)(?=<|$)/g)]
                        .map(match => match[1].trim())
                        .filter(Boolean);
                }

                let newHTML = langHTML;

                p.forEach(j => {
                    const lang = getLang(j);

                    if (lang) {
                        const ltext = lang_filter_text(lang);
                        const itext = lang_filter_text(j);
                        if (itext != ltext) {
                            const ls = lang_countLeadingSpaces(j);
                            const ts = lang_countLeadingSpaces(j);
                            if (langHTML)
                                newHTML = newHTML.replace(j, ' '.repeat(ls) + lang + ' '.repeat(ts));
                            else
                                newHTML = ' '.repeat(ls) + lang + ' '.repeat(ts);
                        }
                    }
                });

                i.innerHTML = newHTML;
            }
            catch (e) {
                console.error(e);
            }
        }
    }


    const selectors = document.getElementsByClassName('lang-select');

    for (const i of selectors) {
        if (!i._lang_listener) {
            i._lang_listener = true;
            i.addEventListener('click', () => {
                const ident = i.getAttribute('lang-ident');
                setCurrentLangByIdent(ident, null, true, selectors);
            });
        }
    }

    const currentLang = getCurrentLangItem();

    if (currentLang) {

        const currents = document.getElementsByClassName('lang-current');

        for (const i of currents) {
            i.textContent = currentLang.name;
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
    return null;
}

function lang_get_by_cookie() {
    console.log('cookie', lang_getCookie('lang_ident'));
    return lang_getCookie('lang_ident');
}

document.addEventListener('DOMContentLoaded', function () {
    loadLang(() => {
        scan_lang();
        let ident = lang_get_by_loc();
        if (!ident)
            ident = lang_get_by_cookie();
        if (!ident)
            ident = Lang_DefaultIdent;
        if (ident)
            setCurrentLangByIdent(ident);
        lang_loop();
        setInterval(lang_loop, 500);
    });
});