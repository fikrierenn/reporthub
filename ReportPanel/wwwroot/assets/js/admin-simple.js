// Admin Sayfası - Basit JavaScript İşlemleri
// Function/class kullanmadan, sadece basit kodlar

console.log('Admin JavaScript yüklendi!');

// Sayfa hazır olduğunda
document.addEventListener('DOMContentLoaded', function() {
    console.log('Admin DOM hazır!');
});

// Veri Kaynağı İşlemleri
function addDataSource() {
    console.log('Yeni veri kaynağı ekleme');
    document.getElementById('dataSourceModal').classList.remove('hidden');
    document.getElementById('dsModalTitle').textContent = 'Yeni Veri Kaynağı';
    document.getElementById('dsForm').reset();
}

function editDataSource(id) {
    console.log('Veri kaynağı düzenleme:', id);
    document.getElementById('dataSourceModal').classList.remove('hidden');
    document.getElementById('dsModalTitle').textContent = 'Veri Kaynağı Düzenle';
    
    // Basit mock veri
    if (id === 'MAIN') {
        document.getElementById('dsKey').value = 'MAIN';
        document.getElementById('dsTitle').value = 'Ana Veritabanı';
        document.getElementById('dsConn').value = 'Server=localhost;Database=MainDB;';
        document.getElementById('dsActive').checked = true;
    }
}

function deleteDataSource(id) {
    console.log('Veri kaynağı silme:', id);
    if (confirm('Silmek istediğinizden emin misiniz?\n\n' + id)) {
        alert('Silindi: ' + id);
        location.reload();
    }
}

function testDataSource(id) {
    console.log('Veri kaynağı test:', id);
    const btn = event.target.closest('button');
    const oldText = btn.innerHTML;
    
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Test...';
    btn.disabled = true;
    
    setTimeout(() => {
        btn.innerHTML = oldText;
        btn.disabled = false;
        alert('Test başarılı: ' + id);
    }, 2000);
}

// Rapor İşlemleri
function addReport() {
    console.log('Yeni rapor ekleme');
    document.getElementById('reportModal').classList.remove('hidden');
    document.getElementById('rptModalTitle').textContent = 'Yeni Rapor';
    document.getElementById('rptForm').reset();
}

function editReport(id) {
    console.log('Rapor düzenleme:', id);
    document.getElementById('reportModal').classList.remove('hidden');
    document.getElementById('rptModalTitle').textContent = 'Rapor Düzenle';
    
    // Basit mock veri
    document.getElementById('rptTitle').value = 'Örnek Rapor ' + id;
    document.getElementById('rptDesc').value = 'Rapor açıklaması';
}

function deleteReport(id) {
    console.log('Rapor silme:', id);
    if (confirm('Raporu silmek istediğinizden emin misiniz?\n\nID: ' + id)) {
        alert('Rapor silindi: ' + id);
        location.reload();
    }
}

function testReport(id) {
    console.log('Rapor test:', id);
    const btn = event.target.closest('button');
    const oldText = btn.innerHTML;
    
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Test...';
    btn.disabled = true;
    
    setTimeout(() => {
        btn.innerHTML = oldText;
        btn.disabled = false;
        alert('Rapor test başarılı: ' + id);
    }, 2000);
}

// Modal Kapatma
function closeModal(modalId) {
    document.getElementById(modalId).classList.add('hidden');
}

// Form Kaydetme
function saveDataSource() {
    const key = document.getElementById('dsKey').value;
    const title = document.getElementById('dsTitle').value;
    
    if (!key || !title) {
        alert('Lütfen tüm alanları doldurun!');
        return;
    }
    
    alert('Kaydedildi: ' + title);
    closeModal('dataSourceModal');
    location.reload();
}

function saveReport() {
    const title = document.getElementById('rptTitle').value;
    
    if (!title) {
        alert('Lütfen rapor başlığını girin!');
        return;
    }
    
    alert('Rapor kaydedildi: ' + title);
    closeModal('reportModal');
    location.reload();
}