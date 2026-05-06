(function () {
    'use strict';

    if (window.__JellyfinAvatarsInjected) return;
    window.__JellyfinAvatarsInjected = true;

    var STYLE = '\n' +
        '#avatars-modal { position: fixed; inset: 0; background: rgba(0,0,0,0.7); z-index: 9999;\n' +
        '   display: none; align-items: flex-start; justify-content: center; padding: 4vh 2vw; overflow-y: auto; }\n' +
        '#avatars-modal.is-open { display: flex; }\n' +
        '#avatars-modal-card { background: #181818; color: #eee; border-radius: 12px; max-width: 920px; width: 100%;\n' +
        '   padding: 1.4em 1.4em 1.6em; box-shadow: 0 18px 48px rgba(0,0,0,0.5); }\n' +
        '#avatars-modal-header { display: flex; align-items: center; gap: 0.8em; margin-bottom: 0.8em; }\n' +
        '#avatars-modal-title { margin: 0; flex: 1; font-size: 1.25em; }\n' +
        '#avatars-modal-close { background: none; border: 0; color: #ddd; font-size: 1.6em; cursor: pointer; padding: 0 0.4em; }\n' +
        '#avatars-modal-search { width: 100%; min-height: 2.4em; padding: 0.4em 0.7em; border-radius: 6px;\n' +
        '   border: 1px solid rgba(255,255,255,0.2); background: rgba(0,0,0,0.3); color: #eee; margin-bottom: 0.8em; }\n' +
        '#avatars-modal-tabs { display: flex; gap: 0.4em; overflow-x: auto; padding-bottom: 0.6em; margin-bottom: 0.8em; }\n' +
        '#avatars-modal-tabs button { flex: 0 0 auto; padding: 0.45em 0.85em; border-radius: 999px;\n' +
        '   border: 1px solid rgba(255,255,255,0.2); background: transparent; color: #eee; cursor: pointer;\n' +
        '   white-space: nowrap; font-size: 0.9em; }\n' +
        '#avatars-modal-tabs button.is-active { background: #52B54B; border-color: #52B54B; color: #fff; }\n' +
        '#avatars-modal-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(88px, 1fr));\n' +
        '   gap: 0.6em; max-height: 60vh; overflow-y: auto; }\n' +
        '.avatars-modal-card { position: relative; aspect-ratio: 1/1; border-radius: 10px; overflow: hidden;\n' +
        '   border: 2px solid transparent; cursor: pointer; background: #222;\n' +
        '   filter: grayscale(0.55) opacity(0.85); transition: transform 0.12s, filter 0.12s, border-color 0.12s; }\n' +
        '.avatars-modal-card:hover { transform: translateY(-2px); filter: none; }\n' +
        '.avatars-modal-card.is-selected { border-color: #52B54B; filter: none; box-shadow: 0 0 0 2px rgba(82,181,75,0.3); }\n' +
        '.avatars-modal-card img { width: 100%; height: 100%; object-fit: cover; }\n' +
        '#avatars-modal-status { padding: 0.7em 0 0; opacity: 0.75; font-size: 0.9em; min-height: 1.5em; }\n' +
        '#avatars-modal-footer { display: flex; gap: 0.6em; margin-top: 1em; }\n' +
        '#avatars-modal-footer button { padding: 0.5em 1em; border-radius: 6px; cursor: pointer;\n' +
        '   border: 1px solid rgba(255,255,255,0.2); background: rgba(255,255,255,0.05); color: #eee; }\n' +
        '#avatars-trigger { display: inline-flex; align-items: center; gap: 0.4em; margin: 0.4em 0;\n' +
        '   padding: 0.5em 0.9em; border-radius: 6px; cursor: pointer;\n' +
        '   border: 1px solid rgba(82,181,75,0.5); background: rgba(82,181,75,0.15); color: #eee; }\n' +
        '#avatars-trigger:hover { background: rgba(82,181,75,0.3); }\n';

    function injectStyle() {
        if (document.getElementById('avatars-style')) return;
        var s = document.createElement('style');
        s.id = 'avatars-style';
        s.textContent = STYLE;
        document.head.appendChild(s);
    }

    function apiUrl(path) {
        if (window.ApiClient && typeof ApiClient.getUrl === 'function') {
            return ApiClient.getUrl(path);
        }
        return '/' + path.replace(/^\/+/, '');
    }

    function authFetch(path, options) {
        options = options || {};
        options.headers = Object.assign({}, options.headers || {});
        if (window.ApiClient && typeof ApiClient.accessToken === 'function') {
            options.headers['X-Emby-Token'] = ApiClient.accessToken();
        }
        return fetch(apiUrl(path), options);
    }

    function getCurrentUserId() {
        if (window.ApiClient && typeof ApiClient.getCurrentUserId === 'function') {
            return ApiClient.getCurrentUserId();
        }
        return null;
    }

    function escapeHtml(s) {
        var div = document.createElement('div');
        div.textContent = s == null ? '' : String(s);
        return div.innerHTML;
    }

    function buildModal() {
        if (document.getElementById('avatars-modal')) return;
        var modal = document.createElement('div');
        modal.id = 'avatars-modal';
        modal.innerHTML =
            '<div id="avatars-modal-card" role="dialog" aria-modal="true">' +
                '<div id="avatars-modal-header">' +
                    '<h2 id="avatars-modal-title">Choose an avatar</h2>' +
                    '<button id="avatars-modal-close" aria-label="Close">×</button>' +
                '</div>' +
                '<input id="avatars-modal-search" type="search" placeholder="Search…" />' +
                '<div id="avatars-modal-tabs"></div>' +
                '<div id="avatars-modal-grid"></div>' +
                '<div id="avatars-modal-status"></div>' +
                '<div id="avatars-modal-footer">' +
                    '<button type="button" id="avatars-modal-remove">Remove avatar</button>' +
                '</div>' +
            '</div>';
        document.body.appendChild(modal);

        modal.addEventListener('click', function (e) { if (e.target === modal) closeModal(); });
        modal.querySelector('#avatars-modal-close').addEventListener('click', closeModal);
        modal.querySelector('#avatars-modal-search').addEventListener('input', function (e) {
            state.query = e.target.value || '';
            renderGrid();
        });
        modal.querySelector('#avatars-modal-remove').addEventListener('click', removeAvatar);
    }

    var state = {
        categories: [],
        avatars: [],
        activeCategory: null,
        query: '',
        selectedKey: null,
        userId: null,
    };

    function avatarKey(a) { return a.kind + ':' + a.id; }
    function setStatus(text) {
        var el = document.getElementById('avatars-modal-status');
        if (el) el.textContent = text || '';
    }

    function openModal() {
        buildModal();
        document.getElementById('avatars-modal').classList.add('is-open');
        state.userId = getCurrentUserId();
        loadAll();
    }
    function closeModal() {
        var m = document.getElementById('avatars-modal');
        if (m) m.classList.remove('is-open');
    }

    function loadAll() {
        setStatus('Loading…');
        Promise.all([
            authFetch('Avatars/Catalog/Categories').then(function (r) { return r.json(); }),
            authFetch('Avatars/Catalog/Avatars').then(function (r) { return r.json(); }),
            state.userId
                ? authFetch('Avatars/User/' + encodeURIComponent(state.userId)).then(function (r) { return r.ok ? r.json() : null; })
                : Promise.resolve(null),
        ]).then(function (results) {
            state.categories = results[0] || [];
            state.avatars = results[1] || [];
            var mapping = results[2];
            if (mapping && mapping.kind && mapping.avatarId) {
                state.selectedKey = mapping.kind + ':' + mapping.avatarId;
            }
            if (state.categories.length > 0) {
                state.activeCategory = state.categories[0].id;
            }
            renderTabs();
            renderGrid();
        }).catch(function (err) {
            setStatus('Could not load: ' + err.message);
        });
    }

    function renderTabs() {
        var el = document.getElementById('avatars-modal-tabs');
        if (!el) return;
        el.innerHTML = '';
        state.categories.forEach(function (cat) {
            var btn = document.createElement('button');
            btn.type = 'button';
            btn.className = state.activeCategory === cat.id ? 'is-active' : '';
            btn.textContent = cat.displayName + ' (' + cat.count + ')';
            btn.addEventListener('click', function () {
                state.activeCategory = cat.id;
                renderTabs();
                renderGrid();
            });
            el.appendChild(btn);
        });
    }

    function renderGrid() {
        var el = document.getElementById('avatars-modal-grid');
        if (!el) return;
        var query = state.query.toLowerCase();
        var filtered = state.avatars.filter(function (a) {
            if (state.activeCategory && a.categoryId !== state.activeCategory) return false;
            if (!query) return true;
            return a.displayName.toLowerCase().indexOf(query) !== -1
                || a.id.toLowerCase().indexOf(query) !== -1;
        });

        el.innerHTML = '';
        if (filtered.length === 0) {
            setStatus('No avatars match.');
            return;
        }
        setStatus(filtered.length + ' avatar' + (filtered.length > 1 ? 's' : ''));

        filtered.forEach(function (a) {
            var card = document.createElement('div');
            card.className = 'avatars-modal-card' + (state.selectedKey === avatarKey(a) ? ' is-selected' : '');
            card.title = a.displayName;
            card.innerHTML = '<img loading="lazy" src="' + a.url + '" alt="' + escapeHtml(a.displayName) + '" />';
            card.addEventListener('click', function () { applyAvatar(a); });
            el.appendChild(card);
        });
    }

    function applyAvatar(a) {
        if (!state.userId) return;
        setStatus('Applying ' + a.displayName + '…');
        authFetch('Avatars/User/Set', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ kind: a.kind, avatarId: a.id, userId: state.userId }),
        }).then(function (r) {
            if (!r.ok) throw new Error('HTTP ' + r.status);
            state.selectedKey = avatarKey(a);
            renderGrid();
            setStatus('Applied. Reload the page to see your new avatar.');
            forceProfileImageRefresh();
        }).catch(function (err) {
            setStatus('Failed: ' + err.message);
        });
    }

    function removeAvatar() {
        if (!state.userId) return;
        setStatus('Removing avatar…');
        authFetch('Avatars/User/Remove', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ userId: state.userId }),
        }).then(function (r) {
            if (!r.ok) throw new Error('HTTP ' + r.status);
            state.selectedKey = null;
            renderGrid();
            setStatus('Avatar cleared.');
            forceProfileImageRefresh();
        }).catch(function (err) {
            setStatus('Failed: ' + err.message);
        });
    }

    function forceProfileImageRefresh() {
        var ts = Date.now();
        document.querySelectorAll('img').forEach(function (img) {
            var src = img.getAttribute('src');
            if (!src) return;
            if (src.indexOf('/Users/') !== -1 && src.indexOf('/Images/Primary') !== -1) {
                img.src = src.split('?')[0] + '?cb=' + ts;
            }
        });
    }

    function maybeInjectTrigger() {
        var pathOrHash = (location.hash || location.pathname).toLowerCase();
        if (pathOrHash.indexOf('userprofile') === -1 && pathOrHash.indexOf('myprofile') === -1) {
            var existing = document.getElementById('avatars-trigger');
            if (existing) existing.remove();
            return;
        }
        if (document.getElementById('avatars-trigger')) return;

        var anchor = document.querySelector('.selectImageContainer')
                  || document.querySelector('#btnDeleteImage')
                  || document.querySelector('.imageContainer');
        if (!anchor) return;

        var btn = document.createElement('button');
        btn.id = 'avatars-trigger';
        btn.type = 'button';
        btn.innerHTML = '<span style="font-size:1.1em">🖼️</span> Choose from Gallery';
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            openModal();
        });

        anchor.parentNode.insertBefore(btn, anchor.nextSibling);
    }

    injectStyle();

    document.addEventListener('viewshow', maybeInjectTrigger);
    window.addEventListener('hashchange', maybeInjectTrigger);
    document.addEventListener('DOMContentLoaded', maybeInjectTrigger);
    setTimeout(maybeInjectTrigger, 500);
})();
