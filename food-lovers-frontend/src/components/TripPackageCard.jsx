// src/components/TripPackageCard.jsx
import { useState } from "react";
import { createBooking } from "../services/api";

function TripPackageCard({ tripPackage, isLoggedIn }) {
  if (!tripPackage) return null;

  const {
    tripPackageName,
    tripPackageDescription,
    countryName,
    cities,
    pois,
    tripPackageId,
  } = tripPackage;

  const [showForm, setShowForm] = useState(false);
  const [checkin, setCheckin] = useState("");
  const [checkout, setCheckout] = useState("");
  const [travelers, setTravelers] = useState(1);
  const [bookingStatus, setBookingStatus] = useState(null);
  const [bookingError, setBookingError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setBookingStatus(null);
    setBookingError(null);

    if (!isLoggedIn) {
      setBookingError("You must be logged in to book this trip.");
      return;
    }

    if (!tripPackageId) {
      setBookingError("This trip is missing an ID and cannot be booked.");
      return;
    }

    try {
      setSubmitting(true);
      const result = await createBooking({
        packageId: tripPackageId,
        checkin,
        checkout,
        numberOfTravelers: Number(travelers),
        status: "pending",
      });

      setBookingStatus(`Booking created with id ${result.id}`);
      setShowForm(false);
      // optional: reset form fields
      setCheckin("");
      setCheckout("");
      setTravelers(1);
    } catch (err) {
      console.error(err);
      setBookingError(err.message || "Booking failed.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div
      style={{
        display: "flex",
        justifyContent: "center",
      }}
    >
      <div
        style={{
          border: "1px solid #ddd",
          borderRadius: "8px",
          padding: "1.5rem",
          marginBottom: "1.5rem",
          boxShadow: "0 2px 4px rgba(0,0,0,0.05)",
          width: "25rem",
          backdropFilter: "brightness(20%)",
        }}
      >
        <h2>{tripPackageName}</h2>
        <p>{tripPackageDescription}</p>
        <p>
          <strong>Country:</strong> {countryName}
        </p>

        <h3>Cities & Hotels</h3>
        <ul>
          {cities?.map((city) => (
            <li key={city.cityName}>
              <strong>{city.cityName}</strong> – {city.cityDescription}
              <br />
              Hotel: {city.hotelName} ({city.stars}★, {city.distanceToCenter} km
              to center)
            </li>
          ))}
        </ul>

        <h3>Points of Interest</h3>
        <ul>
          {pois?.map((poi) => (
            <li key={poi.poiName}>
              {poi.poiName} ({poi.poiDistance} km)
            </li>
          ))}
        </ul>

        {/* Booking section */}
        <div style={{ marginTop: "1rem" }}>
          {!showForm ? (
            <button
              onClick={() => setShowForm(true)}
              disabled={!isLoggedIn}
              style={{
                padding: "0.5rem 1rem",
                borderRadius: "6px",
                border: "1px solid #333",
                cursor: isLoggedIn ? "pointer" : "not-allowed",
                opacity: isLoggedIn ? 1 : 0.6,
              }}
            >
              {isLoggedIn ? "Book this trip" : "Login to book"}
            </button>
          ) : (
            <form onSubmit={handleSubmit} style={{ marginTop: "1rem" }}>
              <div style={{ marginBottom: "0.5rem" }}>
                <label style={{ display: "block", marginBottom: "0.25rem" }}>
                  Check-in
                </label>
                <input
                  type="date"
                  value={checkin}
                  onChange={(e) => setCheckin(e.target.value)}
                  required
                />
              </div>

              <div style={{ marginBottom: "0.5rem" }}>
                <label style={{ display: "block", marginBottom: "0.25rem" }}>
                  Check-out
                </label>
                <input
                  type="date"
                  value={checkout}
                  onChange={(e) => setCheckout(e.target.value)}
                  required
                />
              </div>

              <div style={{ marginBottom: "0.5rem" }}>
                <label style={{ display: "block", marginBottom: "0.25rem" }}>
                  Travelers
                </label>
                <input
                  type="number"
                  min="1"
                  value={travelers}
                  onChange={(e) => setTravelers(e.target.value)}
                  required
                />
              </div>

              <button
                type="submit"
                disabled={submitting}
                style={{
                  padding: "0.5rem 1rem",
                  borderRadius: "6px",
                  border: "1px solid #333",
                  cursor: "pointer",
                  marginRight: "0.5rem",
                }}
              >
                {submitting ? "Booking..." : "Confirm booking"}
              </button>
              <button
                type="button"
                onClick={() => setShowForm(false)}
                style={{
                  padding: "0.5rem 1rem",
                  borderRadius: "6px",
                  border: "1px solid #999",
                  cursor: "pointer",
                }}
              >
                Cancel
              </button>
            </form>
          )}

          {/* ✅ Always-visible feedback below button/form */}
          {bookingError && (
            <p style={{ color: "red", marginTop: "0.75rem" }}>{bookingError}</p>
          )}
          {bookingStatus && (
            <p style={{ color: "lightgreen", marginTop: "0.75rem" }}>
              {bookingStatus}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}

export default TripPackageCard;
