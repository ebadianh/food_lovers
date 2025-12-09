// src/hooks/useTripPackages.js
import { useEffect, useMemo, useState } from "react";
import { fetchSearchings } from "../services/api";

export function useTripPackages() {
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    async function load() {
      try {
        setLoading(true);
        setError(null);
        const data = await fetchSearchings();
        setRows(data);
      } catch (err) {
        console.error("Failed to load trip packages", err);
        setError(err);
      } finally {
        setLoading(false);
      }
    }

    load();
  }, []);

  // Group DB rows into nice trip package objects
  const packages = useMemo(() => {
    const map = {};

    for (const row of rows) {
      const key = row.tripPackageName;

      if (!map[key]) {
        map[key] = {
          tripPackageId: row.tripPackageId,
          tripPackageName: row.tripPackageName,
          tripPackageDescription: row.tripPackageDescription,
          countryName: row.countryName,
          cities: {}, // map by city
          pois: {}, // map by poi
        };
      }

      // Cities
      if (!map[key].cities[row.city]) {
        map[key].cities[row.city] = {
          cityName: row.city,
          cityDescription: row.cityDescription,
          hotelName: row.hotelName,
          stars: row.stars,
          distanceToCenter: row.distanceToCenter,
        };
      }

      // POIs
      if (!map[key].pois[row.poiName]) {
        map[key].pois[row.poiName] = {
          poiName: row.poiName,
          poiDistance: row.poiDistance,
        };
      }
    }

    return Object.values(map).map((pkg) => ({
      ...pkg,
      cities: Object.values(pkg.cities),
      pois: Object.values(pkg.pois),
    }));
  }, [rows]);

  return { packages, loading, error };
}
