javascript EcoLoop\wwwroot\js\map.js
// map.js — deferred initialization and ensure map tiles show (invalidateSize)
document.addEventListener('DOMContentLoaded', function () {
    const endpoint = '/Store/GetStores';

    const categoryColors = {
        "Еко храни": "#2fb26b",
        "Натурална козметика": "#2a9e62",
        "Еко облекло": "#3c8bd1",
        "Напитки": "#d18b3c",
        "Продукти за дома": "#9e6bbf",
        "*": "#4a4a4a"
    };

    const mapEl = document.getElementById('map');
    if (!mapEl) return;

    const map = L.map('map', { zoomControl: true }).setView([42.7, 23.3], 7);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    // Force Leaflet to compute size after layout is stable
    setTimeout(() => map.invalidateSize(), 300);

    let markers = L.layerGroup().addTo(map);
    let storesData = [];

    const categoryButtons = document.querySelectorAll('#categoryFilters .category-badge');
    const distanceRange = document.getElementById('distanceRange');
    const distanceValue = document.getElementById('distanceValue');
    const filterOwn = document.getElementById('filterOwnPackaging');
    const filterOpenNow = document.getElementById('filterOpenNow');
    const filterRating = document.getElementById('filterRating');
    const applyBtn = document.getElementById('applyFilters');
    const resetBtn = document.getElementById('resetFilters');

    let selectedCategory = '*';
    categoryButtons.forEach(b => {
        b.addEventListener('click', () => {
            categoryButtons.forEach(x => x.style.boxShadow = 'none');
            b.style.boxShadow = '0 4px 14px rgba(23,40,27,0.08)';
            selectedCategory = b.getAttribute('data-cat') || '*';
        });
    });

    if (distanceRange) distanceRange.addEventListener('input', () => {
        if (distanceValue) distanceValue.textContent = distanceRange.value + ' km';
    });

    applyBtn && applyBtn.addEventListener('click', () => applyFilters());
    resetBtn && resetBtn.addEventListener('click', () => {
        selectedCategory = '*';
        categoryButtons.forEach(x => x.style.boxShadow = 'none');
        if (distanceRange) { distanceRange.value = 25; if (distanceValue) distanceValue.textContent = '25 km'; }
        if (filterOwn) filterOwn.checked = false;
        if (filterOpenNow) filterOpenNow.checked = false;
        if (filterRating) filterRating.value = 0;
        applyFilters();
    });

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
        let dayLabel = null;
        if (day >= 1 && day <= 5) dayLabel = 'Пон-Пет';
        else if (day === 6) dayLabel = 'Съб';
        else dayLabel = 'Нед';
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

    function renderMarkers(list) {
        markers.clearLayers();
        list.forEach(s => {
            try {
                const latVal = (s.latitude !== undefined) ? s.latitude : s.Latitude;
                const lngVal = (s.longitude !== undefined) ? s.longitude : s.Longitude;
                const lat = parseFloat(latVal);
                const lng = parseFloat(lngVal);
                if (!Number.isFinite(lat) || !Number.isFinite(lng)) return;
                const color = categoryColors[s.category] || categoryColors['*'] || '#4a4a4a';
                const circle = L.circleMarker([lat, lng], {
                    radius: 8, fillColor: color, color: '#fff', weight: 2, fillOpacity: 0.9
                });
                const popupHtml = `
                    <div style="min-width:200px">
                        <div class="popup-title">${s.name}</div>
                        <div class="small-muted">${s.category || ''} — ${s.rating ?? ''}★</div>
                        <div style="margin-top:6px;">${s.shortDescription ? s.shortDescription : ''}</div>
                        <div style="margin-top:8px;">
                            <a class="btn btn-outline-success" href="/Store/Details/${s.id}">Виж детайли</a>
                        </div>
                    </div>`;
                circle.bindPopup(popupHtml);
                circle.addTo(markers);
            } catch (err) {
                console.warn('Marker render error', err);
            }
        });

        // allow browser to finish layout then refresh map size & optionally fit bounds
        setTimeout(() => {
            try { map.invalidateSize(); } catch (e) { }
        }, 250);
    }

    async function loadStores() {
        try {
            const res = await fetch(endpoint);
            storesData = await res.json();
            applyFilters();
            // ensure tiles render after data load
            setTimeout(() => { try { map.invalidateSize(); } catch (e) { } }, 300);
        } catch (e) {
            console.error('Failed to load stores', e);
        }
    }

    function applyFilters() {
        const center = map.getCenter();
        let filtered = (storesData || []).slice();
        if (selectedCategory && selectedCategory !== '*') {
            filtered = filtered.filter(s => (s.category || s.Category || '').indexOf(selectedCategory) !== -1);
        }
        if (filterOwn && filterOwn.checked) {
            filtered = filtered.filter(s => s.acceptsOwnPackaging === true || s.acceptsOwnPackaging === 1 || s.AcceptsOwnPackaging === true);
        }
        const minRating = parseFloat((filterRating && filterRating.value) || 0) || 0;
        filtered = filtered.filter(s => (s.rating || s.Rating || 0) >= minRating);
        const maxKm = parseFloat((distanceRange && distanceRange.value) || 9999) || 9999;
        filtered = filtered.filter(s => {
            const latVal = (s.latitude !== undefined) ? s.latitude : s.Latitude;
            const lngVal = (s.longitude !== undefined) ? s.longitude : s.Longitude;
            if (!latVal || !lngVal) return false;
            const d = distanceKm(center.lat, center.lng, parseFloat(latVal), parseFloat(lngVal));
            return d <= maxKm;
        });
        if (filterOpenNow && filterOpenNow.checked) {
            filtered = filtered.filter(s => isOpenNow(s.workingHours || s.WorkingHours));
        }
        renderMarkers(filtered);
        if (filtered.length) {
            try {
                const markersForBounds = filtered.map(s => [parseFloat((s.latitude !== undefined) ? s.latitude : s.Latitude), parseFloat((s.longitude !== undefined) ? s.longitude : s.Longitude)]);
                const group = L.featureGroup(markersForBounds.map(c => L.marker(c)));
                if (filtered.length <= 12) map.fitBounds(group.getBounds().pad(0.3));
            } catch { }
        }
    }

    function tryGeolocate() {
        if (!navigator.geolocation) return;
        navigator.geolocation.getCurrentPosition(pos => {
            const lat = pos.coords.latitude, lng = pos.coords.longitude;
            map.setView([lat, lng], 12);
            L.circle([lat, lng], { radius: 40, color: '#2fb26b', fillOpacity: 0.08 }).addTo(map);
            applyFilters();
            setTimeout(() => { try { map.invalidateSize(); } catch (e) { } }, 200);
        }, () => { }, { enableHighAccuracy: false, timeout: 5000 });
    }

    loadStores().then(() => tryGeolocate());
});