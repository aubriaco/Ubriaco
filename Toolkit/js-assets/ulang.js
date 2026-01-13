

const ULANG = {
    dict: {},
    select: null,

    load() {
        let xhr = new XMLHttpRequest();
        xhr.onreadystatechange = function () {
            if (this.readyState == 4) {
                if (this.status == 200) {
                    ULANG.dict = JSON.parse(xhr.responseText);
                    ULANG.dom();
                }
            }
        };
        xhr.open('GET', '/lang/dict.json');
        xhr.send(null);
    },

    dom() {
        let select = document.createElement('select');
        select.innerHTML = '<option value="en">English</option><option value="es">Spanish</option>';
        this.select = select;

        let lang = document.createElement('div');
        lang.style.position = 'fixed';
        lang.style.top = '1rem';
        lang.style.right = '1rem';
        lang.style.zIndex = '999';
        lang.appendChild(select);

        
        document.body.appendChild(lang);

        select.addEventListener('change', function (e) {
            ULANG.changeLang(e.target.value);
        });

        ULANG.checkDefault();
    },

    checkDefault() {
        let lang = ULANG.getCookie('u-lang');
        if (lang) {
            ULANG.changeLang(lang, true);
            this.select.value = lang;
        }
    },

    initLang() {
        let eles = document.querySelectorAll('.u-lang');
        eles.forEach(function (i) {
            i.setAttribute('u-lang-value', i.innerHTML.trim());
        });
    },

    changeLang(lang) {
        let eles = document.querySelectorAll('.u-lang');
        eles.forEach(function (i) {
            var val = i.getAttribute('u-lang-value');
            if (ULANG.dict[val] && ULANG.dict[val][lang])
                i.innerHTML = ULANG.dict[val][lang];
        });
        ULANG.setCookie("u-lang", lang);
    },

    setCookie(name, value, days) {
        const expires = new Date(Date.now() + days * 864e5).toUTCString();
        document.cookie = `${name}=${encodeURIComponent(value)}; expires=${expires}; path=/`;
    },

    getCookie(name) {
        const value = document.cookie
            .split('; ')
            .find(row => row.startsWith(name + '='));
        return value ? decodeURIComponent(value.split('=')[1]) : null;
    }

};


document.addEventListener('DOMContentLoaded', function () {
    ULANG.initLang();
    ULANG.load();


});