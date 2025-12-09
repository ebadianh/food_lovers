// src/services/auth.js

const API_BASE_URL = "http://localhost:5240";

export async function login(email, password) {
  const res = await fetch(`${API_BASE_URL}/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include", // send/receive session cookie
    body: JSON.stringify({ email, password }),
  });

  if (!res.ok) {
    throw new Error(`Login failed: HTTP ${res.status}`);
  }

  // Your backend Login.Post returns a bool
  const ok = await res.json();
  return ok === true;
}

export async function logout() {
  const res = await fetch(`${API_BASE_URL}/login`, {
    method: "DELETE",
    credentials: "include",
  });

  if (!res.ok) {
    throw new Error(`Logout failed: HTTP ${res.status}`);
  }
}

export async function checkLoginStatus() {
  const res = await fetch(`${API_BASE_URL}/login`, {
    method: "GET",
    credentials: "include",
  });

  if (!res.ok) {
    throw new Error(`Check login failed: HTTP ${res.status}`);
  }

  const loggedIn = await res.json(); // bool
  return loggedIn === true;
}
