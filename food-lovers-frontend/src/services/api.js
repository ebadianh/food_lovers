// src/services/api.js
const API_BASE_URL = "http://localhost:5240";

// ...your existing fetchSearchings etc.

export async function createBooking({
  packageId,
  checkin,
  checkout,
  numberOfTravelers,
  status = "pending",
}) {
  const res = await fetch(`${API_BASE_URL}/bookings`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    body: JSON.stringify({
      packageId,
      checkin,
      checkout,
      numberOfTravelers,
      status,
    }),
  });

  if (res.status === 401) {
    throw new Error("You must be logged in to make a booking.");
  }

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(`Booking failed: HTTP ${res.status} ${text}`);
  }

  const data = await res.json(); // e.g. 4
  const id = typeof data === "number" ? data : data?.id;

  return { id }; // normalize so caller can always use result.id
}

export async function fetchSearchings() {
  const res = await fetch("http://localhost:5240/searchings", {
    credentials: "include",
  });

  if (!res.ok) {
    throw new Error(`HTTP ${res.status}`);
  }

  return await res.json();
}
