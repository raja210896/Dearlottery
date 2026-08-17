import { api } from "./client";

export const notificationsApi = {
  publicKey: () => api.get<{ publicKey: string }>("/notifications/public-key"),
  subscribe: (payload: {
    endpoint: string; p256dh: string; auth: string;
    drawTimePreference?: string | null; resultNotify: boolean; analysisNotify: boolean; dailyReminder: boolean;
  }) => api.post<void>("/notifications/subscribe", payload),
  unsubscribe: (endpoint: string) => api.post<void>("/notifications/unsubscribe", { endpoint }),
};

function urlBase64ToUint8Array(base64: string): ArrayBuffer {
  const padding = "=".repeat((4 - (base64.length % 4)) % 4);
  const base64Safe = (base64 + padding).replace(/-/g, "+").replace(/_/g, "/");
  const raw = atob(base64Safe);
  return Uint8Array.from([...raw].map((c) => c.charCodeAt(0))).buffer as ArrayBuffer;
}

export async function subscribeToPush(prefs: {
  drawTimePreference?: string | null; resultNotify: boolean; analysisNotify: boolean; dailyReminder: boolean;
}) {
  if (!("serviceWorker" in navigator) || !("PushManager" in window)) {
    throw new Error("Push notifications are not supported in this browser.");
  }
  const registration = await navigator.serviceWorker.ready;
  const { publicKey } = await notificationsApi.publicKey();
  if (!publicKey) throw new Error("Push notifications are not configured on the server yet.");

  let subscription = await registration.pushManager.getSubscription();
  if (!subscription) {
    subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(publicKey),
    });
  }

  const json = subscription.toJSON();
  await notificationsApi.subscribe({
    endpoint: json.endpoint!,
    p256dh: json.keys!.p256dh!,
    auth: json.keys!.auth!,
    ...prefs,
  });
}

export async function unsubscribeFromPush() {
  if (!("serviceWorker" in navigator)) return;
  const registration = await navigator.serviceWorker.ready;
  const subscription = await registration.pushManager.getSubscription();
  if (subscription) {
    await notificationsApi.unsubscribe(subscription.endpoint);
    await subscription.unsubscribe();
  }
}
