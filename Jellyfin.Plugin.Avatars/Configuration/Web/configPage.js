(function () {
    'use strict';

    var PLUGIN_GUID = 'c0a3f7d2-1b94-4e08-9a1f-7d2e8b6c4f10';

    var page = document.querySelector('.avatars-admin');
    if (!page) return;

    var statsBuiltIn = page.querySelector('[data-stat-builtin]');
    var statsUploaded = page.querySelector('[data-stat-uploaded]');
    var statsCategories = page.querySelector('[data-stat-categories]');
    var categoriesEl = page.querySelector('[data-avatars-categories]');
    var uploadedEl = page.querySelector('[data-avatars-uploaded]');
    var statusEl = page.querySelector('[data-avatars-status]');
    var uploadZone = page.querySelector('[data-avatars-uploadzone]');
    var fileInput = page.querySelector('[data-avatars-fileinput]');

    function setStatus(text, isError) {
        statusEl.style.color = isError ? '#ff8a8a' : '';
        statusEl.textContent = text || '';
    }

    function escapeHtml(s) {
        var div = document.createElement('div');
        div.textContent = s == null ? '' : String(s);
        return div.innerHTML;
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

    function loadConfig() {
        if (!window.ApiClient || typeof ApiClient.getPluginConfiguration !== 'function') {
            return Promise.reject(new Error('ApiClient unavailable'));
        }
        return ApiClient.getPluginConfiguration(PLUGIN_GUID);
    }

    function saveConfig(config) {
        return ApiClient.updatePluginConfiguration(PLUGIN_GUID, config);
    }

    function renderCategories(config, categories) {
        var disabledSet = {};
        (config.DisabledBuiltInIds || []).forEach(function (id) { disabledSet[id] = true; });

        // Build a per-category disabled flag: a category is "disabled" if all its avatars are disabled.
        // Toggling the category checkbox flips all its avatar ids in DisabledBuiltInIds.
        // We need the avatar ids per category, fetch the full Avatars list once.
        return authFetch('Avatars/Catalog/Avatars').then(function (r) { return r.json(); }).then(function (allAvatars) {
            var byCategory = {};
            allAvatars.forEach(function (a) {
                if (a.kind !== 'BuiltIn') return;
                if (!byCategory[a.categoryId]) byCategory[a.categoryId] = [];
                byCategory[a.categoryId].push(a.id);
            });

            categoriesEl.innerHTML = '';
            categories.forEach(function (cat) {
                if (cat.id === 'uploaded') return; // Uploads aren't toggleable here.
                var ids = byCategory[cat.id] || [];
                var visibleIds = ids.filter(function (id) { return !disabledSet[id]; });
                var enabled = visibleIds.length > 0;

                var row = document.createElement('div');
                row.className = 'avatars-cat-card';
                row.innerHTML =
                    '<input type="checkbox" id="cat-' + escapeHtml(cat.id) + '"' + (enabled ? ' checked' : '') + ' />' +
                    '<label for="cat-' + escapeHtml(cat.id) + '">' + escapeHtml(cat.displayName) + '</label>' +
                    '<span class="count">' + ids.length + '</span>';
                row.querySelector('input').addEventListener('change', function (e) {
                    toggleCategory(cat.id, ids, e.target.checked);
                });
                categoriesEl.appendChild(row);
            });
        });
    }

    function toggleCategory(catId, ids, enabled) {
        loadConfig().then(function (config) {
            var disabledSet = {};
            (config.DisabledBuiltInIds || []).forEach(function (id) { disabledSet[id] = true; });

            ids.forEach(function (id) {
                if (enabled) {
                    delete disabledSet[id];
                } else {
                    disabledSet[id] = true;
                }
            });

            config.DisabledBuiltInIds = Object.keys(disabledSet);
            return saveConfig(config);
        }).then(function () {
            setStatus('Category "' + catId + '" ' + (enabled ? 'enabled' : 'disabled') + '.');
            refresh();
        }).catch(function (err) {
            setStatus('Save failed: ' + err.message, true);
        });
    }

    function renderUploaded(uploaded) {
        statsUploaded.textContent = uploaded.length;
        uploadedEl.innerHTML = '';
        if (uploaded.length === 0) {
            uploadedEl.innerHTML = '<div class="hint" style="grid-column: 1/-1;">No custom uploads yet.</div>';
            return;
        }
        uploaded.forEach(function (a) {
            var card = document.createElement('div');
            card.className = 'avatars-uploaded-card';
            card.title = a.displayName;
            card.innerHTML =
                '<img loading="lazy" src="' + escapeHtml(a.url) + '" alt="' + escapeHtml(a.displayName) + '" />' +
                '<button class="delete-btn" type="button" aria-label="Delete">×</button>' +
                '<span class="label">' + escapeHtml(a.displayName) + '</span>';
            card.querySelector('.delete-btn').addEventListener('click', function () { deleteUpload(a.id); });
            uploadedEl.appendChild(card);
        });
    }

    function deleteUpload(id) {
        if (!confirm('Delete this avatar from the pool? Users currently using it will keep their picture until they pick another.')) return;
        authFetch('Avatars/Upload/' + encodeURIComponent(id), { method: 'DELETE' })
            .then(function (r) {
                if (!r.ok) throw new Error('HTTP ' + r.status);
                setStatus('Deleted.');
                refresh();
            }).catch(function (err) { setStatus('Delete failed: ' + err.message, true); });
    }

    function uploadFiles(files) {
        if (!files || files.length === 0) return;
        setStatus('Uploading ' + files.length + ' file' + (files.length > 1 ? 's' : '') + '…');
        var ops = Array.prototype.map.call(files, function (file) {
            var fd = new FormData();
            fd.append('file', file);
            return authFetch('Avatars/Upload', { method: 'POST', body: fd })
                .then(function (r) { return r.ok ? r.json() : Promise.reject(new Error(file.name + ': HTTP ' + r.status)); });
        });
        Promise.all(ops).then(function () {
            setStatus('Upload complete.');
            refresh();
        }).catch(function (err) {
            setStatus(err.message, true);
            refresh();
        });
    }

    function refresh() {
        return Promise.all([
            loadConfig(),
            authFetch('Avatars/Catalog/Categories').then(function (r) { return r.json(); }),
            authFetch('Avatars/Catalog/Avatars?categoryId=uploaded').then(function (r) { return r.json(); }),
            authFetch('Avatars/Catalog/Avatars').then(function (r) { return r.json(); }),
        ]).then(function (results) {
            var config = results[0];
            var categories = results[1] || [];
            var uploaded = results[2] || [];
            var allAvatars = results[3] || [];

            statsCategories.textContent = categories.filter(function (c) { return c.id !== 'uploaded'; }).length;
            statsBuiltIn.textContent = allAvatars.filter(function (a) { return a.kind === 'BuiltIn'; }).length;

            renderUploaded(uploaded);
            return renderCategories(config, categories);
        }).catch(function (err) {
            setStatus('Could not load: ' + err.message, true);
        });
    }

    // Drag-and-drop handlers
    ['dragenter', 'dragover'].forEach(function (ev) {
        uploadZone.addEventListener(ev, function (e) {
            e.preventDefault();
            uploadZone.classList.add('is-drag');
        });
    });
    ['dragleave', 'drop'].forEach(function (ev) {
        uploadZone.addEventListener(ev, function (e) {
            e.preventDefault();
            uploadZone.classList.remove('is-drag');
        });
    });
    uploadZone.addEventListener('drop', function (e) {
        if (e.dataTransfer && e.dataTransfer.files) uploadFiles(e.dataTransfer.files);
    });
    fileInput.addEventListener('change', function (e) {
        uploadFiles(e.target.files);
        fileInput.value = '';
    });

    // Wait for the dashboard's pageshow signal so ApiClient is bound.
    document.addEventListener('pageshow', refresh);
    if (window.ApiClient) refresh();
})();
