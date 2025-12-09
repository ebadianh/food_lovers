// src/components/TripPackageCard.jsx

function TripPackageCard({ tripPackage }) {
  const { tripPackageName, tripPackageDescription, countryName, cities, pois } =
    tripPackage;

  return (
    <div
      className="card-container"
      style={{
        display: "flex",
        alignItems: "center",
        alignContent: "center",
        justifyContent: "center",
      }}
    >
      <div
        style={{
          border: "2px solid #ddd",
          borderRadius: "8px",
          padding: "1.5rem",
          marginBottom: "1.5rem",
          boxShadow: "0 2px 4px rgba(0,0,0,0.05)",
          width: "500px",
        }}
      >
        <h2>{tripPackageName}</h2>
        <p>{tripPackageDescription}</p>
        <p>
          <strong>Country:</strong> {countryName}
        </p>

        <h3>Cities & Hotels</h3>
        <ul>
          {cities.map((city) => (
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
          {pois.map((poi) => (
            <li key={poi.poiName}>
              {poi.poiName} ({poi.poiDistance} km)
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

export default TripPackageCard;
