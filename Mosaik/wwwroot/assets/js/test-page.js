(() => {
    window.addSampleData = async () => {
        try {
            const response = await fetch('/Test/AddSampleData', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            const result = await response.json();

            if (result.success) {
                alert('Basarili: ' + result.message);
                location.reload();
            } else {
                alert('Hata: ' + result.message);
            }
        } catch (error) {
            alert('Hata: ' + error.message);
        }
    };
})();
