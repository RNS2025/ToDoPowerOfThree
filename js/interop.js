function scrollToElementId(elementId) {
  const element = document.getElementById(elementId);
  if (element) {
    element.scrollIntoView({ behavior: "smooth", block: "center" });
  }
}

// PWA Notification and DB functions
const PWA_DB_NAME = "PwaToDoDB"; // Must match service-worker.js
const PWA_STORE_NAME = "todayTasksStore"; // Must match service-worker.js
const PWA_TASKS_KEY = "todayPowerOfThreeTasks"; // Must match service-worker.js

function openAppDB() {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(PWA_DB_NAME, 1); // Version must match SW
    request.onupgradeneeded = (event) => {
      const db = event.target.result;
      if (!db.objectStoreNames.contains(PWA_STORE_NAME)) {
        db.createObjectStore(PWA_STORE_NAME, { keyPath: "id" });
      }
    };
    request.onsuccess = (event) => resolve(event.target.result);
    request.onerror = (event) => {
      console.error("App: DB error:", event.target.error);
      reject(event.target.error);
    };
  });
}

window.pwaUpdateTodayTasksInDB = async (tasks) => {
  if (!tasks) {
    console.warn("App: pwaUpdateTodayTasksInDB called with null tasks.");
    return;
  }
  try {
    const db = await openAppDB();
    const transaction = db.transaction(PWA_STORE_NAME, "readwrite");
    const store = transaction.objectStore(PWA_STORE_NAME);

    // Ensure tasks are plain objects for IndexedDB
    const simplifiedTasks = tasks.map((task) => ({
      title: task.title,
      isCompleted: task.isCompleted,
      // Add any other fields the service worker might need
    }));

    await new Promise((resolve, reject) => {
      const putRequest = store.put({
        id: PWA_TASKS_KEY,
        tasks: simplifiedTasks,
      });
      putRequest.onsuccess = resolve;
      putRequest.onerror = reject;
    });

    transaction.oncomplete = () => {
      console.log("App: Today's tasks updated in IndexedDB for PWA.");
      // Notify the service worker that data has changed so it can check immediately if needed
      if (navigator.serviceWorker && navigator.serviceWorker.controller) {
        navigator.serviceWorker.controller.postMessage({
          type: "TRIGGER_NOTIFICATION_CHECK",
        });
      }
    };
    transaction.onerror = (event) => {
      console.error(
        "App: Error saving tasks to IndexedDB:",
        event.target.error
      );
    };
  } catch (error) {
    console.error("App: Failed to update tasks in IndexedDB:", error);
  }
};

window.pwaRequestNotificationPermission = async () => {
  if (!("Notification" in window)) {
    console.warn("Browser does not support desktop notification.");
    return "notsupported";
  }
  const permission = await Notification.requestPermission();
  console.log("Notification permission status:", permission);
  if (
    permission === "granted" &&
    navigator.serviceWorker &&
    navigator.serviceWorker.controller
  ) {
    // Trigger an immediate check if permission is granted now
    navigator.serviceWorker.controller.postMessage({
      type: "TRIGGER_NOTIFICATION_CHECK",
    });
  }
  return permission;
};

window.pwaGetNotificationPermissionState = () => {
  if (!("Notification" in window)) return "default"; // Or "notsupported"
  return Notification.permission;
};

window.pwaShowTestNotification = async () => {
  if (!("Notification" in window)) {
    console.warn("Browser does not support desktop notification.");
    alert("Notifications are not supported by this browser.");
    return;
  }

  if (Notification.permission === "granted") {
    try {
      const registration = await navigator.serviceWorker.ready;
      if (registration && registration.showNotification) {
        registration.showNotification("📢 Test Notification", {
          body: "This is a test notification from PowerOfThree!",
          icon: "/favicon.png", // Ensure this path is correct
          tag: "powerofthree-test-notification",
        });
        console.log("Test notification shown.");
      } else {
        console.warn(
          "Service worker not ready or showNotification not available on registration."
        );
        alert(
          "Could not show test notification. Service worker might not be active."
        );
      }
    } catch (error) {
      console.error("Error showing test notification:", error);
      alert("Error showing test notification: " + error.message);
    }
  } else if (Notification.permission === "denied") {
    alert(
      "Notification permission has been denied. Please enable it in your browser settings."
    );
  } else {
    alert("Please grant notification permission first.");
    // Optionally, you could trigger the permission request here again:
    // await window.pwaRequestNotificationPermission();
  }
};
