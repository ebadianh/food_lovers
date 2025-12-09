// src/components/LoginForm.jsx
import { useState } from "react";
import "../index.css";

function LoginForm({ onLogin, loading, error }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await onLogin(email, password);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div style={{ display: "flex", justifyContent: "center" }}>
      <form
        onSubmit={handleSubmit}
        style={{
          border: "1px solid #ddd",
          borderRadius: "8px",
          padding: "4rem",
          maxWidth: "320px",
          marginBottom: "1.5rem",
        }}
      >
        <h2>Login</h2>

        <div style={{ marginBottom: "0.5rem" }}>
          <label style={{ display: "block", marginBottom: "0.25rem" }}>
            Email
          </label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            style={{ width: "100%", padding: "0.25rem" }}
          />
        </div>

        <div style={{ marginBottom: "0.5rem" }}>
          <label style={{ display: "block", marginBottom: "0.25rem" }}>
            Password
          </label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            style={{ width: "100%", padding: "0.25rem" }}
          />
        </div>

        {error && (
          <p style={{ color: "red", marginBottom: "0.5rem" }}>{error}</p>
        )}

        <button
          className="login-btn"
          style={{
            marginTop: "10px",
            borderRadius: "5px",
            padding: "5px",
            width: "5rem",
            alignContent: "center",
            fontWeight: "bold",
            fontSize: "15px",
            border: "1px solid black",
            cursor: "pointer",
          }}
          type="submit"
          disabled={submitting || loading}
        >
          {submitting || loading ? "Logging in..." : "Log in"}
        </button>
      </form>
    </div>
  );
}

export default LoginForm;
