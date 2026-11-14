// VOXIE QUEST - Offline Service Worker (PWA)
// Enables offline play, caching of Blazor WebAssembly assets, and versioned updates

const CACHE_NAME = 'voxie-quest-v1.0.0';

const PRECACHE_ASSETS = [
    './',
    'index.html',
    'favicon.png',
    'icon-192.png',
    'manifest.json',
    'css/app.css?v=2.2',
    'css/storybook.css?v=2.2',
    'js/voxieInterop.js?v=2.2',
    'js/voice.js?v=2.2',
    'js/voskInterop.js?v=2.2'
];

// Install: Pre-cache application shell assets
self.addEventListener('install', event => {
    self.skipWaiting();
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            console.log('[VoxieSW] Pre-caching core application shell...');
            return cache.addAll(PRECACHE_ASSETS).catch(err => {
                console.warn('[VoxieSW] Some precache assets failed to load:', err);
            });
        })
    );
});

// Activate: Clean up older cache versions and take immediate control
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys => {
            return Promise.all(
                keys.map(key => {
                    if (key !== CACHE_NAME) {
                        console.log('[VoxieSW] Purging outdated cache:', key);
                        return caches.delete(key);
                    }
                })
            );
        }).then(() => self.clients.claim())
    );
});

// Fetch: Network-first for navigation, Cache-first for WebAssembly framework assets
self.addEventListener('fetch', event => {
    const request = event.request;

    // Only intercept GET requests
    if (request.method !== 'GET') return;

    const url = new URL(request.url);

    // 1. Navigation requests (HTML pages) -> Network-First with cache fallback
    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request)
                .then(response => {
                    if (response && response.status === 200) {
                        const copy = response.clone();
                        caches.open(CACHE_NAME).then(cache => cache.put('index.html', copy));
                    }
                    return response;
                })
                .catch(() => {
                    return caches.match('index.html').then(cached => {
                        return cached || caches.match('./');
                    });
                })
        );
        return;
    }

    // 2. Framework & static assets -> Cache-First with network fallback & dynamic cache
    event.respondWith(
        caches.match(request).then(cachedResponse => {
            if (cachedResponse) {
                return cachedResponse;
            }

            return fetch(request).then(networkResponse => {
                // Cache valid same-origin responses (especially _framework/*.wasm and data/*.json)
                if (networkResponse && networkResponse.status === 200 && url.origin === self.location.origin) {
                    const responseToCache = networkResponse.clone();
                    caches.open(CACHE_NAME).then(cache => {
                        cache.put(request, responseToCache);
                    });
                }
                return networkResponse;
            });
        })
    );
});
