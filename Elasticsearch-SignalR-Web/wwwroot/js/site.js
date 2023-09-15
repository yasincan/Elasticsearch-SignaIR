

var map = L.map('map').setView([0, 0], 2); // Dünya haritası başlangıç merkezi ve zoom seviyesi

L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
}).addTo(map);



// Bir işaretleme oluşturmak için:
function createMarker(lat, lng, popupContent) {
    var marker = L.marker([lat, lng]).addTo(map);
    marker.bindPopup(popupContent);
}


var markers = [];

// Elasticsearch verilerini kullanarak işaretlemeleri ekleyin
function addMarker(lat, lng, popupContent) {
    if (markers.length >= 30) {
        // 30'den fazla işaretleme varsa en eski işarelemeyi kaldırın
        var oldestMarker = markers.shift(); // Dizinin başından kaldırın
        map.removeLayer(oldestMarker);
    }

    var marker = L.marker([lat, lng]).addTo(map);
    marker.bindPopup(popupContent);

    markers.push(marker); // Yeni işarelemeyi diziye ekleyin
}


const connection = new signalR.HubConnectionBuilder()
    .withUrl("saelMapHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("updateMarker", function (lat, lng, popupContent) {
    addMarker(lat, lng, popupContent)
});

connection.start().catch(function (err) {
    console.error(err.toString());
});
