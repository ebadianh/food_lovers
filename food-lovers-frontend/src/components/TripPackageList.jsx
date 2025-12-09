// src/components/TripPackageList.jsx
import TripPackageCard from "./TripPackageCard";

function TripPackageList({ packages }) {
  if (!packages.length) {
    return <p>No trips found.</p>;
  }

  return (
    <div>
      {packages.map((pkg) => (
        <TripPackageCard
          key={pkg.tripPackageName} // unique per package name
          tripPackage={pkg}
        />
      ))}
    </div>
  );
}

export default TripPackageList;
