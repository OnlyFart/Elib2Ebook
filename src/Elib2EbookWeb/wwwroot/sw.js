const CACHE_NAME = 'elib2ebook-v1';

const STATIC_ASSETS = [
    '/',
    '/app.css',
    '/Elib2EbookWeb.styles.css',
    '/manifest.json',
    '/favicon.png',
    '/icons/icon32x32.png',
    '/icons/icon64x64.png',
    '/icons/icon128x128.png',
    '/icons/icon256x256.png',
    '/icons/icon512x512.png',
];

// Install event — cache static assets
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll(STATIC_ASSETS);
        })
    );
    self.skipWaiting();
});

// Activate event — clean old caches
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames
                    .filter(name => name !== CACHE_NAME)
                    .map(name => caches.delete(name))
            );
        })
    );
    self.clients.claim();
});

// Fetch event — network-first strategy for pages, cache-first for static assets
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);

    // Skip non-GET requests
    if (request.method !== 'GET') return;

    // Skip blazor resources and external URLs
    if (url.pathname.startsWith('/_framework/') ||
        url.pathname.startsWith('/_content/') ||
        url.origin !== self.location.origin) {
        return;
    }

    // For static assets — cache-first
    if (STATIC_ASSETS.includes(url.pathname) ||
        url.pathname.startsWith('/icons/')) {
        event.respondWith(
            caches.match(request).then(cached => {
                return cached || fetch(request).then(response => {
                    return caches.open(CACHE_NAME).then(cache => {
                        cache.put(request, response.clone());
                        return response;
                    });
                });
            })
        );
        return;
    }

    // For navigation requests — network-first
    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request)
                .then(response => {
                    return caches.open(CACHE_NAME).then(cache => {
                        cache.put(request, response.clone());
                        return response;
                    });
                })
                .catch(() => {
                    return caches.match(request).then(cached => {
                        return cached || caches.match('/');
                    });
                })
        );
        return;
    }

    // Default: network-first for other requests
    event.respondWith(
        fetch(request)
            .then(response => {
                return caches.open(CACHE_NAME).then(cache => {
                    cache.put(request, response.clone());
                    return response;
                });
            })
            .catch(() => caches.match(request))
    );
});
