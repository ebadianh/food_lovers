// src/components/TripPackageList.jsx
import TripPackageCard from "./TripPackageCard";

function TripPackageList({ packages, isLoggedIn }) {
  if (!packages.length) {
    return <p>No trips found.</p>;
  }

  return (
    <div>
      {packages.map((pkg) => (
        <TripPackageCard
          key={pkg.tripPackageName}
          tripPackage={pkg}
          isLoggedIn={isLoggedIn} // fixed
        />
      ))}
    </div>
  );
}

export default TripPackageList;
