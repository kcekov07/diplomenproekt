document.addEventListener('DOMContentLoaded', async function () {
    const endpoint = '/Store/GetStores';

    const mapEl = document.getElementById('map');
    if (!mapEl) return;

    const map = L.map('map', { zoomControl: true }).setView([42.7, 23.3], 10);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    setTimeout(() => { try { map.invalidateSize(); } catch (e) { } }, 250);

    let storesData = [];
    let markers = L.layerGroup().addTo(map);
    let userLocation = null;
    let userMarker = null;

    const mf = window.mapFilters;

    // --- HELPERS ---
    const distanceKm = (lat1, lon1, lat2, lon2) => {
        const R = 6371;
        const dLat = (lat2 - lat1) * Math.PI / 180;
        const dLon = (lon2 - lon1) * Math.PI / 180;
        const a = Math.sin(dLat / 2) ** 2 +
            Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
            Math.sin(dLon / 2) ** 2;
        return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    };

    const isOpenNow = (workingHoursText) => {
        if (!workingHoursText) return false;
        const now = new Date();
        const day = now.getDay();
        const hhmm = now.getHours().toString().padStart(2, '0') + ':' + now.getMinutes().toString().padStart(2, '0');
        let dayLabel = (day >= 1 && day <= 5) ? 'Пон-Пет' : (day === 6 ? 'Съб' : 'Нед');
        const parts = workingHoursText.split(';').map(p => p.trim());
        for (const p of parts) {
            if (p.startsWith(dayLabel + ':')) {
                const m = p.match(/(\d{1,2}:\d{2})\s*-\s*(\d{1,2}:\d{2})/);
                if (!m) return false;
                return hhmm >= m[1] && hhmm <= m[2];
            }
        }
        return false;
    };

    const makeStoreIcon = () => L.divIcon({ html: '<div style="font-size:24px;color:#2E7D32;">📍</div>', className: 'store-div-icon', iconSize: [24, 24], iconAnchor: [12, 24] });
    const makeUserIcon = () => L.divIcon({ html: '<div style="font-size:22px;">👤</div>', className: 'user-div-icon', iconSize: [22, 22], iconAnchor: [11, 22] });

    const createPopupHtml = (s) => {
        const img = s.imageUrl || s.ImageUrl || '';
        return `<div class="store-popup">
            <div class="store-popup-main">
                ${img ? `<div class="popup-img-wrap"><img class="popup-img" src="${img}" alt="${s.name || s.Name}"/></div>` : ''}
                <div class="store-popup-text">
                    <div class="store-popup-title">${s.name || s.Name}</div>
                    <div class="store-popup-meta">${s.category || s.Category} ${s.rating ? '— ' + s.rating + '★' : ''}</div>
                    <div class="store-popup-desc">${s.shortDescription || s.ShortDescription || ''}</div>
                </div>
            </div>
            <div class="store-popup-hours">${s.workingHours || s.WorkingHours || ''}</div>
            <div class="store-popup-address">${s.address || s.Address || ''}</div>
            <div class="store-popup-actions">
                <a class="popup-btn-green" href="/Store/Details/${s.id || s.Id}">🔎 Виж детайли</a>
                <button class="popup-btn-outline" onclick="alert('Маршрут...')">🧭 Маршрут</button>
            </div>
        </div>`;
    };

    // --- RENDERING ---
    const renderMarkers = (list) => {
        markers.clearLayers();
        list.forEach(s => {
            const lat = parseFloat(s.latitude || s.Latitude);
            const lng = parseFloat(s.longitude || s.Longitude);
            if (!isFinite(lat) || !isFinite(lng)) return;
            const marker = L.marker([lat, lng], { icon: makeStoreIcon() }).addTo(markers);
            marker.bindPopup(createPopupHtml(s));
        });
    };

    const renderStoresList = (list) => {
        if (!mf.storesListEl) return;
        mf.storesListEl.innerHTML = '';
        if (!list.length) { mf.storesListEl.innerHTML = '<div class="small-muted">Няма магазини.</div>'; return; }

        const grid = document.createElement('div');
        grid.className = 'store-list-grid';
        list.forEach(s => {
            const card = document.createElement('div');
            card.className = 'store-card';
            card.innerHTML = `
                <h4>${s.name || s.Name}</h4>
                <div class="meta">${s.category || s.Category} ${s.rating ? '— ' + s.rating + '★' : ''}</div>
                <div class="desc">${s.shortDescription || s.ShortDescription || ''}</div>
                <div class="actions">
                    <a class="popup-btn-green" href="/Store/Details/${s.id || s.Id}">Виж</a>
                    <button class="popup-btn-outline">Покажи на картата</button>
                </div>`;
            card.querySelector('button').addEventListener('click', () => {
                const lat = parseFloat(s.latitude || s.Latitude);
                const lng = parseFloat(s.longitude || s.Longitude);
                if (isFinite(lat) && isFinite(lng)) {
                    map.setView([lat, lng], 14);
                    markers.eachLayer(m => { if (m.getLatLng && Math.abs(m.getLatLng().lat - lat) < 1e-6 && Math.abs(m.getLatLng().lng - lng) < 1e-6) m.openPopup(); });
                }
            });
            grid.appendChild(card);
        });
        mf.storesListEl.appendChild(grid);
    };

    const readFilters = () => ({
        category: mf.categoryEl?.value || '*',
        maxKm: Number(mf.distanceEl?.value || 9999),
        onlyOwn: !!mf.ownEl?.checked,
        openNow: !!mf.openEl?.checked,
        delivery: !!mf.deliveryEl?.checked,
        refill: !!mf.refillEl?.checked,
        minRating: Number(mf.ratingEl?.value || 0),
        tags: Array.from(mf.tagsEl?.selectedOptions || []).map(o => o.value)
    });

    const applyFilters = () => {
        const f = readFilters();
        const c = userLocation || map.getCenter();
        const filtered = storesData.filter(s => {
            const lat = parseFloat(s.latitude || s.Latitude);
            const lng = parseFloat(s.longitude || s.Longitude);
            if (!isFinite(lat) || !isFinite(lng)) return false;
            if (f.category !== '*' && !(s.category || s.Category || '').includes(f.category)) return false;
            if (f.onlyOwn && !(s.acceptsOwnPackaging || s.AcceptsOwnPackaging)) return false;
            if (f.delivery && !(s.hasDelivery || s.HasDelivery)) return false;
            if (f.refill && !(s.hasRefillStation || s.HasRefillStation)) return false;
            if (f.openNow && !isOpenNow(s.workingHours || s.WorkingHours)) return false;
            const rating = Number(s.rating || s.Rating || 0);
            if (rating < f.minRating) return false;
            if (f.tags.length && !f.tags.every(t => (s.ecoTags || s.EcoTags || '').split(',').includes(t))) return false;
            if (distanceKm(c.lat, c.lng, lat, lng) > f.maxKm) return false;
            return true;
        });
        renderMarkers(filtered);
        renderStoresList(filtered);
        mf.resultsEl.innerHTML = filtered.length ? `${filtered.length} магазина намерени.` : '<div class="small-muted">Няма намерени магазини.</div>';
        if (filtered.length) {
            const bounds = L.featureGroup(filtered.map(s => L.marker([parseFloat(s.latitude || s.Latitude), parseFloat(s.longitude || s.Longitude)]))).getBounds();
            map.fitBounds(bounds.pad(0.25));
        }
    };

    const showUserMarker = (lat, lng) => {
        userLocation = { lat, lng };
        if (userMarker) map.removeLayer(userMarker);
        userMarker = L.marker([lat, lng], { icon: makeUserIcon(), zIndexOffset: 1000 }).addTo(map);
        userMarker.bindPopup('<strong>Вие сте тук</strong>').openPopup();
    };

    const tryGeolocate = () => {
        if (!navigator.geolocation) return;
        navigator.geolocation.getCurrentPosition(pos => {
            const lat = pos.coords.latitude, lng = pos.coords.longitude;
            map.setView([lat, lng], 13);
            showUserMarker(lat, lng);
            applyFilters(); // филтри спрямо местоположението
        }, err => { console.warn('Geolocation error', err); }, { timeout: 7000 });
    };

    // --- EVENTS: автоматично при промяна ---
    [mf.categoryEl, mf.distanceEl, mf.ownEl, mf.openEl, mf.deliveryEl, mf.refillEl, mf.ratingEl, mf.tagsEl].forEach(el => {
        el?.addEventListener('change', applyFilters);
    });

    // reset filters
    document.getElementById('btnReset')?.addEventListener('click', () => {
        if (mf) {
            mf.categoryEl.value = '*'; mf.distanceEl.value = '9999'; mf.ownEl.checked = false;
            mf.openEl.checked = false; mf.deliveryEl.checked = false; mf.refillEl.checked = false; mf.ratingEl.value = '0';
            Array.from(mf.tagsEl.options).forEach(o => o.selected = false);
        }
        applyFilters();
    });

    // --- INITIAL LOAD ---
    try {
        const res = await fetch(endpoint);
        storesData = await res.json();
        renderMarkers(storesData);
        renderStoresList(storesData);
        mf.resultsEl.innerHTML = `${storesData.length} магазина заредени.`;
        tryGeolocate();
    } catch (e) { console.error('Failed to fetch stores', e); }
});
