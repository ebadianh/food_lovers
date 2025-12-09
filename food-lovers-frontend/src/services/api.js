// src/services/api.js

const API_BASE_URL = "http://localhost:5240";

export async function fetchSearchings() {
  const res = await fetch(`${API_BASE_URL}/searchings`, {
    credentials: "include",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch searchings: HTTP ${res.status}`);
  }

  return res.json();
}
