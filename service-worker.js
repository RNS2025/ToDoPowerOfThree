const CACHE_NAME = "powerofthree-cache-v1";
const DB_NAME = "PwaToDoDB";
const STORE_NAME = "todayTasksStore";
const TASKS_KEY = "todayPowerOfThreeTasks";
let notificationInterval;

// Basic caching for offline capability (optional for notifications but good for PWAs)
self.addEventListener("install", (event) => {
  console.log("Service Worker: Installing...");
  event.waitUntil(
    caches
      .open(CACHE_NAME)
      .then((cache) => {
        // Add essential files to cache for offline mode if desired
        // console.log('Service Worker: Caching app shell');
        // return cache.addAll([
        //   self.registration.scope || '/ThePowerOfThree/', // Ensure base path is correct
        //   self.registration.scope + 'index.html',
        //   self.registration.scope + 'css/app.css',
        //   // Add other critical assets
        // ]);
      })
      .then(() => {
        console.log("Service Worker: Skip waiting on install");
        return self.skipWaiting();
      })
      .catch((error) => {
        console.error("Service Worker: Error during install event:", error);
      })
  );
});

self.addEventListener("activate", (event) => {
  console.log("Service Worker: Activated.");
  event.waitUntil(
    Promise.all([
      clients.claim(), // clients.claim() returns a Promise
      caches.keys().then((cacheNames) => {
        return Promise.all(
          cacheNames.map((cache) => {
            if (cache !== CACHE_NAME) {
              console.log("Service Worker: Clearing old cache:", cache);
              return caches.delete(cache);
            }
            return Promise.resolve(); // Explicitly return a resolved promise
          })
        );
      }),
    ])
      .then(() => {
        console.log("Service Worker: Clients claimed and old caches cleared.");
        // Setup periodic check after activation is complete
        checkAndNotify(); // Initial check
        if (notificationInterval) {
          clearInterval(notificationInterval);
        }
        notificationInterval = setInterval(checkAndNotify, 60 * 60 * 1000); // Every 60 minutes
      })
      .catch((error) => {
        console.error(
          "Service Worker: Error during activation sequence:",
          error
        );
      })
  );
});

self.addEventListener("fetch", (event) => {
  // Basic cache-first strategy (optional)
  // event.respondWith(
  //     caches.match(event.request).then(response => {
  //         return response || fetch(event.request);
  //     })
  // );
});

function openDB() {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, 1);
    request.onupgradeneeded = (event) => {
      const db = event.target.result;
      if (!db.objectStoreNames.contains(STORE_NAME)) {
        db.createObjectStore(STORE_NAME, { keyPath: "id" });
      }
    };
    request.onsuccess = (event) => resolve(event.target.result);
    request.onerror = (event) => {
      console.error("Service Worker: DB error:", event.target.error);
      reject(event.target.error);
    };
  });
}

async function getTodayTasksFromDB() {
  try {
    const db = await openDB();
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(STORE_NAME, "readonly");
      const store = transaction.objectStore(STORE_NAME);
      const request = store.get(TASKS_KEY);
      request.onsuccess = () =>
        resolve(request.result ? request.result.tasks : []);
      request.onerror = (event) => {
        console.error(
          "Service Worker: Error fetching tasks from DB:",
          event.target.error
        );
        reject(event.target.error);
      };
    });
  } catch (error) {
    console.error("Service Worker: Could not open DB to get tasks:", error);
    return []; // Return empty array on error
  }
}

async function checkAndNotify() {
  console.log("Service Worker: Checking tasks for notification...");
  if (!self.Notification || Notification.permission !== "granted") {
    console.log(
      "Service Worker: Notification permission not granted or Notifications not supported."
    );
    return;
  }

  try {
    const tasks = await getTodayTasksFromDB();
    if (!tasks || tasks.length === 0) {
      console.log(
        'Service Worker: No "Today\'s Power of Three" tasks found in DB or tasks array is empty.'
      );
      return;
    }

    const incompleteTask = tasks.find((task) => !task.isCompleted);

    if (incompleteTask) {
      console.log(
        "Service Worker: Found incomplete task:",
        incompleteTask.title
      );
      self.registration.showNotification("⚡ PowerOfThree Reminder", {
        body: `Don't forget: ${incompleteTask.title}`,
        icon: "/favicon.png", // Ensure this path is correct
        tag: "powerofthree-task-reminder", // So new notifications replace old ones
        renotify: true, // Re-notify even if the tag is the same
      });
    } else {
      console.log(
        'Service Worker: All "Today\'s Power of Three" tasks are completed or no incomplete tasks found.'
      );
    }
  } catch (error) {
    console.error(
      "Service Worker: Error checking tasks or showing notification:",
      error
    );
  }
}

self.addEventListener("message", (event) => {
  if (event.data && event.data.type === "TRIGGER_NOTIFICATION_CHECK") {
    console.log(
      "Service Worker: Received message to trigger notification check."
    );
    checkAndNotify();
  }
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  event.waitUntil(
    clients
      .matchAll({ type: "window", includeUncontrolled: true })
      .then((windowClients) => {
        // Use self.registration.scope to correctly construct the URL
        const appUrl = new URL(self.registration.scope).pathname;
        for (let i = 0; i < windowClients.length; i++) {
          const client = windowClients[i];
          const clientUrl = new URL(client.url).pathname;
          if (clientUrl === appUrl && "focus" in client) {
            return client.focus();
          }
        }
        if (clients.openWindow) {
          return clients.openWindow(appUrl);
        }
      })
  );
});
