# Skeleton surface fraction in run Q, against the polynomial closure it replaces

`run Q` — `f_s = n_C(s)(T_pore, p) / n_C,total`, `T_pore = T_s + 200 K`, **zero fitted constants**. `T_s` is the run's own converged pocket surface temperature, read from `skeleton_layer.json` — so this is the f_s the solver actually used, not a recomputation.

`polynomial` — `PropellantExtensions.GetPocketSurfaceFraction`, i.e. `sum(c_i (p/1e6)^i) / pocket_mass_fraction` over the **22 shipped coefficients**. Those coefficients are byte-identical to Babuk's measured `Z_m^a(p)/Z_p`, so this column is at once the historical closure and the measurement.

| p, MPa | Bas_0 run Q | Bas_0 polynomial | ratio | Bas_1 run Q | Bas_1 polynomial | ratio | Bas_2 run Q | Bas_2 polynomial | ratio | Bas_3 run Q | Bas_3 polynomial | ratio | Bas_4 run Q | Bas_4 polynomial | ratio |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1.00 | 0.5861 | 0.2586 | 2.266 | 0.2325 | 0.3781 | 0.615 | 0.2347 | 0.3589 | 0.654 | 0.2363 | 0.3129 | 0.755 | 0.3665 | 0.5288 | 0.693 |
| 1.61 | 0.5136 | 0.2476 | 2.074 | 0.2051 | 0.3119 | 0.658 | 0.2072 | 0.3596 | 0.576 | 0.2087 | 0.2991 | 0.698 | 0.3454 | 0.5279 | 0.654 |
| 2.22 | 0.4685 | 0.2345 | 1.998 | 0.1896 | 0.2667 | 0.711 | 0.1916 | 0.3567 | 0.537 | 0.1928 | 0.2842 | 0.678 | 0.3342 | 0.5271 | 0.634 |
| 2.83 | 0.4368 | 0.2198 | 1.988 | 0.1792 | 0.2361 | 0.759 | 0.1812 | 0.3505 | 0.517 | 0.1820 | 0.2682 | 0.679 | 0.3271 | 0.5263 | 0.622 |
| 3.44 | 0.4128 | 0.2040 | 2.024 | 0.1716 | 0.2151 | 0.798 | 0.1735 | 0.3416 | 0.508 | 0.1741 | 0.2512 | 0.693 | 0.3221 | 0.5257 | 0.613 |
| 4.06 | 0.3937 | 0.1876 | 2.099 | 0.1658 | 0.2003 | 0.828 | 0.1675 | 0.3303 | 0.507 | 0.1679 | 0.2332 | 0.720 | 0.3183 | 0.5250 | 0.606 |
| 4.67 | 0.3781 | 0.1711 | 2.210 | 0.1610 | 0.1891 | 0.852 | 0.1627 | 0.3173 | 0.513 | 0.1629 | 0.2141 | 0.761 | 0.3153 | 0.5243 | 0.601 |
| 5.28 | 0.3649 | 0.1550 | 2.354 | 0.1571 | 0.1797 | 0.874 | 0.1587 | 0.3029 | 0.524 | 0.1587 | 0.1941 | 0.817 | 0.3128 | 0.5235 | 0.598 |
| 5.89 | 0.3535 | 0.1398 | 2.528 | 0.1538 | 0.1713 | 0.898 | 0.1553 | 0.2875 | 0.540 | 0.1551 | 0.1732 | 0.896 | 0.3108 | 0.5227 | 0.595 |
| 6.50 | 0.3436 | 0.1261 | 2.725 | 0.1509 | 0.1633 | 0.924 | 0.1524 | 0.2717 | 0.561 | 0.1520 | 0.1513 | 1.005 | 0.3090 | 0.5218 | 0.592 |

| composition | T_s converged, K | T_s required, K | mean ratio | RMS relative error |
|---|---:|---:|---:|---:|
| Bas_0 | 830–854 | 584–670 | 2.227 | 124.9 % |
| Bas_1 | 611–631 | 655–712 | 0.792 | 23.1 % |
| Bas_2 | 613–634 | 701–800 | 0.544 | 45.8 % |
| Bas_3 | 621–640 | 640–779 | 0.770 | 25.1 % |
| Bas_4 | 600–620 | 738–912 | 0.621 | 38.0 % |
