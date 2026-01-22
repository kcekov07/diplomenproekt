// map.js — map + filters + stores list
// Behavior:
// - On page load: initialize tiles, try geolocation and show user as "👤" icon,
//   load ALL stores and render them as pin icons (no filters applied).
// - Filters / "Магазини около мен" still work and will refine visible markers/list.
// - Reset now truly resets UI and shows ALL stores on map + list.

document.addEventListener('DOMContentLoaded', function () {
    const endpoint = '/Store/GetStores';

    const categoryColors = {
        "Еко храни": "#2E7D32",
        "Натурална козметика": "#2A9E62",
        "Еко облекло": "#3C8BD1",
        "Напитки": "#D18B3C",
        "Продукти за дома": "#9E6BBF",
        "*": "#4A4A4A"
    };

    const mapEl = document.getElementById('map');
    if (!mapEl) return;

    const map = L.map('map', { zoomControl: true }).setView([42.7, 23.3], 10);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);
    setTimeout(() => { try { map.invalidateSize(); } catch (e) { } }, 250);

    let markers = L.layerGroup().addTo(map);
    let storesData = [];
    let userLocation = null;
    let userMarker = null;

    const btnApply = document.getElementById('btnApply');
    const btnReset = document.getElementById('btnReset');
    const resultsEl = (window.mapFilters && window.mapFilters.resultsEl) || document.getElementById('resultsList');
    const storesListEl = (window.mapFilters && window.mapFilters.storesListEl) || document.getElementById('storesList');

    function distanceKm(lat1, lon1, lat2, lon2) {
        const R = 6371;
        const dLat = (lat2 - lat1) * Math.PI / 180;
        const dLon = (lon2 - lon1) * Math.PI / 180;
        const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
            Math.sin(dLon / 2) * Math.sin(dLon / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        return R * c;
    }

    function isOpenNow(workingHoursText) {
        if (!workingHoursText) return false;
        const now = new Date();
        const day = now.getDay();
        const hhmm = now.getHours().toString().padStart(2, '0') + ':' + now.getMinutes().toString().padStart(2, '0');
        let dayLabel = (day >= 1 && day <= 5) ? 'Пон-Пет' : (day === 6 ? 'Съб' : 'Нед');
        const parts = workingHoursText.split(';').map(p => p.trim());
        for (const p of parts) {
            if (p.toLowerCase().startsWith(dayLabel.toLowerCase() + ':')) {
                const rhs = p.split(':').slice(1).join(':').trim();
                if (!rhs || rhs.toLowerCase().includes('затворено')) return false;
                const m = rhs.match(/(\d{1,2}:\d{2})\s*-\s*(\d{1,2}:\d{2})/);
                if (m) return hhmm >= m[1] && hhmm <= m[2];
            }
        }
        for (const p of parts) {
            if (p.startsWith('Пон-Пет:') && day >= 1 && day <= 5) {
                const rhs = p.split(':').slice(1).join(':').trim();
                const m = rhs.match(/(\d{1,2}:\d{2})\s*-\s*(\d{1,2}:\d{2})/);
                if (m) return hhmm >= m[1] && hhmm <= m[2];
            }
        }
        return false;
    }

    function createPopupHtml(s) {
        const name = s.name || s.Name || '';
        const cat = s.category || s.Category || '';
        const rating = (s.rating ?? s.Rating) ? (s.rating ?? s.Rating) + '★' : '';
        const desc = s.shortDescription || s.ShortDescription || '';
        const addr = s.address || s.Address || '';
        const img = s.imageUrl || s.ImageUrl || '';
        const imgHtml = img ? `<div class="popup-img-wrap"><img class="popup-img" src="${img}" alt="${name}" /></div>` : '';
        return `
            <div class="store-popup">
                <div class="store-popup-main">
                    ${imgHtml}
                    <div class="store-popup-text">
                        <div class="store-popup-title">${name}</div>
                        <div class="store-popup-meta">${cat}${rating ? ' — ' + rating : ''}</div>
                        <div class="store-popup-desc">${desc}</div>
                    </div>
                </div>
                <div class="store-popup-hours">${s.workingHours || s.WorkingHours || ''}</div>
                <div class="store-popup-address">${addr}</div>
                <div class="store-popup-actions">
                    <a class="popup-btn-green" href="/Store/Details/${s.id || s.Id}"><span class="btn-icon">🔎</span>Виж детайли</a>
                    <button class="popup-btn-outline" onclick="alert('Отваряне на маршрут...')">🧭 Маршрут</button>
                </div>
            </div>`;
    }

    function makeStoreIcon(category) {
        const html = `<div style="font-size:24px;line-height:24px;text-align:center;color:inherit;text-shadow:0 1px 0 rgba(0,0,0,0.15);"><span style="filter: drop-shadow(0 1px 0 rgba(0,0,0,0.15));">📍</span></div>`;
        return L.divIcon({ html, className: 'store-div-icon', iconSize: [24, 24], iconAnchor: [12, 24] });
    }

    function makeUserIcon() {
        const html = `<div style="font-size:22px;line-height:22px;text-align:center;"><span>👤</span></div>`;
        return L.divIcon({ html, className: 'user-div-icon', iconSize: [22, 22], iconAnchor: [11, 22] });
    }

    function renderMarkers(list) {
        markers.clearLayers();
        if (!list || !list.length) return;
        list.forEach(s => {
            const latVal = s.latitude ?? s.Latitude;
            const lngVal = s.longitude ?? s.Longitude;
            const lat = parseFloat(latVal);
            const lng = parseFloat(lngVal);
            if (!Number.isFinite(lat) || !Number.isFinite(lng)) return;
            const icon = makeStoreIcon(s.category || s.Category || '*');
            const m = L.marker([lat, lng], { icon: icon });
            m.bindPopup(createPopupHtml(s));
            m.addTo(markers);
        });
        setTimeout(() => { try { map.invalidateSize(); } catch (e) { } }, 150);
    }

    async function fetchStores() {
        try {
            const res = await fetch(endpoint);
            if (!res.ok) throw new Error('Network response not ok');
            storesData = await res.json();
            return storesData;
        } catch (err) {
            console.error('Failed to fetch stores', err);
            return [];
        }
    }

    function renderResults(list) {
        if (!resultsEl) return;
        if (!list || list.length === 0) {
            resultsEl.innerHTML = '<div class="small-muted">Няма намерени магазини.</div>';
            return;
        }
        resultsEl.innerHTML = '';
        list.forEach(s => {
            const div = document.createElement('div');
            div.className = 'sidebar-result-item';
            div.innerHTML = `<strong>${s.name || s.Name}</strong><div class="small-muted">${s.category || s.Category || ''}</div>`;
            div.addEventListener('click', function () {
                const lat = parseFloat(s.latitude ?? s.Latitude);
                const lng = parseFloat(s.longitude ?? s.Longitude);
                if (!isNaN(lat) && !isNaN(lng)) {
                    map.setView([lat, lng], 14);
                    markers.eachLayer(layer => {
                        if (layer.getLatLng) {
                            const ll = layer.getLatLng();
                            if (Math.abs(ll.lat - lat) < 1e-6 && Math.abs(ll.lng - lng) < 1e-6) {
                                layer.openPopup();
                            }
                        }
                    });
                }
            });
            resultsEl.appendChild(div);
        });
    }

    function renderStoresList(list) {
        if (!storesListEl) return;
        storesListEl.innerHTML = '';
        if (!list || list.length === 0) {
            storesListEl.innerHTML = '<div class="small-muted">Няма магазини.</div>';
            return;
        }

        const grid = document.createElement('div');
        grid.className = 'store-list';
        grid.style.display = 'grid';
        grid.style.gridTemplateColumns = 'repeat(auto-fill,minmax(280px,1fr))';
        grid.style.gap = '12px';

        list.forEach(s => {
            const card = document.createElement('div');
            card.className = 'card';
            card.style.padding = '12px';
            card.style.borderRadius = '8px';
            card.style.border = '1px solid #e6efe7';

            const name = document.createElement('h4');
            name.style.margin = '0 0 6px';
            name.textContent = s.name || s.Name || '—';

            const meta = document.createElement('div');
            meta.className = 'small-muted';
            meta.style.marginBottom = '6px';
            meta.textContent = (s.category || s.Category || '') + (s.rating ? ` — ${s.rating}★` : '');

            const desc = document.createElement('div');
            desc.style.fontSize = '0.95rem';
            desc.style.marginBottom = '8px';
            desc.textContent = s.shortDescription || s.ShortDescription || '';

            const actions = document.createElement('div');
            actions.style.display = 'flex';
            actions.style.gap = '8px';

            const detailsBtn = document.createElement('a');
            detailsBtn.className = 'btn btn-sm btn-outline-success';
            detailsBtn.href = `/Store/Details/${s.id || s.Id}`;
            detailsBtn.textContent = 'Виж';

            const focusBtn = document.createElement('button');
            focusBtn.className = 'btn btn-sm';
            focusBtn.textContent = 'Покажи на картата';
            focusBtn.addEventListener('click', function () {
                const lat = parseFloat(s.latitude ?? s.Latitude);
                const lng = parseFloat(s.longitude ?? s.Longitude);
                if (!isNaN(lat) && !isNaN(lng)) {
                    map.setView([lat, lng], 14);
                    markers.eachLayer(layer => {
                        if (layer.getLatLng) {
                            const ll = layer.getLatLng();
                            if (Math.abs(ll.lat - lat) < 1e-6 && Math.abs(ll.lng - lng) < 1e-6) {
                                layer.openPopup();
                            }
                        }
                    });
                }
            });

            actions.appendChild(detailsBtn);
            actions.appendChild(focusBtn);

            card.appendChild(name);
            card.appendChild(meta);
            card.appendChild(desc);
            card.appendChild(actions);

            grid.appendChild(card);
        });

        storesListEl.appendChild(grid);
    }

    function readFilters() {
        const mf = window.mapFilters || {};
        const category = (mf.categoryEl && mf.categoryEl.value) || '*';
        const maxKm = Number((mf.distanceEl && mf.distanceEl.value) || 9999);
        const onlyOwn = !!(mf.ownEl && mf.ownEl.checked);
        const openNow = !!(mf.openEl && mf.openEl.checked);
        const minRating = Number((mf.ratingEl && mf.ratingEl.value) || 0);
        return { category, maxKm, onlyOwn, openNow, minRating };
    }

    async function applyFiltersAndShow(center) {
        if (!storesData || storesData.length === 0) {
            await fetchStores();
        }
        const filters = readFilters();
        const c = center || map.getCenter();
        const filtered = (storesData || []).filter(s => {
            const latVal = s.latitude ?? s.Latitude;
            const lngVal = s.longitude ?? s.Longitude;
            if (!latVal || !lngVal) return false;
            if (filters.category && filters.category !== '*' && !((s.category || s.Category || '').includes(filters.category))) return false;
            if (filters.onlyOwn && !(s.acceptsOwnPackaging === true || s.AcceptsOwnPackaging === true || s.acceptsOwnPackaging === 1)) return false;
            const rating = Number(s.rating ?? s.Rating ?? 0);
            if (rating < filters.minRating) return false;
            const d = distanceKm(c.lat, c.lng, parseFloat(latVal), parseFloat(lngVal));
            if (d > filters.maxKm) return false;
            if (filters.openNow && !isOpenNow(s.workingHours || s.WorkingHours)) return false;
            return true;
        });
        renderMarkers(filtered);
        renderResults(filtered);
        renderStoresList(filtered); // also update the list below map to show filtered subset
        if (filtered.length > 0) {
            try {
                const bounds = L.featureGroup(filtered.map(s => L.marker([parseFloat(s.latitude ?? s.Latitude), parseFloat(s.longitude ?? s.Longitude)]))).getBounds();
                if (filtered.length <= 12) map.fitBounds(bounds.pad(0.25));
            } catch { }
        }
    }

    function showUserMarker(lat, lng) {
        userLocation = { lat, lng };
        if (userMarker) {
            try { map.removeLayer(userMarker); } catch { }
        }
        // place human icon marker
        const icon = makeUserIcon();
        userMarker = L.marker([lat, lng], { icon: icon, zIndexOffset: 1000 }).addTo(map);
        userMarker.bindPopup('<strong>Вие сте тук</strong>').openPopup();
    }

    function tryGeolocateOnLoad() {
        if (!navigator.geolocation) return;
        navigator.geolocation.getCurrentPosition(pos => {
            const lat = pos.coords.latitude, lng = pos.coords.longitude;
            map.setView([lat, lng], 13);
            showUserMarker(lat, lng);
            setTimeout(() => { try { map.invalidateSize(); } catch (e) { } }, 200);
        }, err => {
            console.warn('Geolocation failed or denied', err);
        }, { enableHighAccuracy: false, timeout: 7000 });
    }

    btnApply && btnApply.addEventListener('click', async function () {
        const center = userLocation ? { lat: userLocation.lat, lng: userLocation.lng } : map.getCenter();
        await applyFiltersAndShow(center);
    });

    btnReset && btnReset.addEventListener('click', function () {
        if (window.mapFilters) {
            window.mapFilters.categoryEl && (window.mapFilters.categoryEl.value = '*');
            window.mapFilters.distanceEl && (window.mapFilters.distanceEl.value = '9999');
            window.mapFilters.ownEl && (window.mapFilters.ownEl.checked = false);
            window.mapFilters.openEl && (window.mapFilters.openEl.checked = false);
            window.mapFilters.ratingEl && (window.mapFilters.ratingEl.value = '0');
        }
        markers.clearLayers();
        if (resultsEl) resultsEl.innerHTML = '<div class="small-muted">Натиснете "Магазини около мен", за да заредите маркери.</div>';
        if (storesListEl) storesListEl.innerHTML = '<div class="small-muted">Натиснете "Магазини около мен", за да заредите резултати.</div>';
    });

    if (resultsEl) resultsEl.innerHTML = '<div class="small-muted">Натиснете "Магазини около мен", за да заредите маркери.</div>';
    if (storesListEl) storesListEl.innerHTML = '<div class="small-muted">Зареждане…</div>';

    // fetch all stores immediately and render the full list under the map AND show pins on the map (no filters applied)
    fetchStores().then(list => {
        renderStoresList(list || []);
        renderResults(list || []);
        renderMarkers(list || []);
        // fit bounds to include all stores and user marker (if geolocated)
        try {
            const pts = (list || []).map(s => [parseFloat(s.latitude ?? s.Latitude), parseFloat(s.longitude ?? s.Longitude)]).filter(p => p && !isNaN(p[0]) && !isNaN(p[1]));
            if (userLocation) pts.push([userLocation.lat, userLocation.lng]);
            if (pts.length) {
                const bounds = L.latLngBounds(pts);
                map.fitBounds(bounds.pad(0.25));
            }
        } catch { }
    });

    tryGeolocateOnLoad();
});