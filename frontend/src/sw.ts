/// <reference lib="webworker" />
import { precacheAndRoute, createHandlerBoundToURL } from "workbox-precaching";
import { registerRoute, NavigationRoute } from "workbox-routing";
import { NetworkFirst } from "workbox-strategies";

declare let self: ServiceWorkerGlobalScope;

precacheAndRoute(self.__WB_MANIFEST);

registerRoute(
  ({ url }) => url.pathname.startsWith("/api/"),
  new NetworkFirst({ cacheName: "api-cache", networkTimeoutSeconds: 5 })
);

// SPA navigation fallback: serve the cached app shell for client-side routes.
const navigationHandler = createHandlerBoundToURL("/index.html");
registerRoute(new NavigationRoute(async (params) => {
  try {
    return await navigationHandler(params);
  } catch {
    const offline = await self.caches.match("/offline.html");
    return offline || Response.error();
  }
}, { denylist: [/^\/api\//] }));

self.addEventListener("push", (event: PushEvent) => {
  if (!event.data) return;
  let payload: { title?: string; body?: string; url?: string } = {};
  try {
    payload = event.data.json();
  } catch {
    payload = { title: "LotteryAnalytics", body: event.data.text() };
  }

  event.waitUntil(
    self.registration.showNotification(payload.title || "LotteryAnalytics", {
      body: payload.body,
      icon: "/icon-192.png",
      badge: "/icon-192.png",
      data: { url: payload.url || "/" },
    })
  );
});

self.addEventListener("notificationclick", (event: NotificationEvent) => {
  event.notification.close();
  const url = (event.notification.data as { url?: string })?.url || "/";
  event.waitUntil(
    self.clients.matchAll({ type: "window" }).then((clients) => {
      for (const client of clients) {
        if ("focus" in client) return (client as WindowClient).focus();
      }
      return self.clients.openWindow(url);
    })
  );
});

self.addEventListener("install", () => self.skipWaiting());
self.addEventListener("activate", () => self.clients.claim());
