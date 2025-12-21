(() => {
    const updateConnectionString = () => {
        const serverName = document.getElementById('serverName')?.value || '';
        const databaseName = document.getElementById('databaseName')?.value || '';
        const authType = document.querySelector('input[name="authType"]:checked')?.value || 'sql';
        const username = document.getElementById('username')?.value || '';
        const password = document.getElementById('password')?.value || '';
        const connStringEl = document.getElementById('connString');

        if (!connStringEl) {
            return;
        }

        if (!serverName || !databaseName) {
            connStringEl.value = '';
            return;
        }

        let connString = `Server=${serverName};Database=${databaseName};`;

        if (authType === 'windows') {
            connString += 'Integrated Security=true;';
        } else {
            connString += `User Id=${username};Password=${password};`;
        }

        connString += 'TrustServerCertificate=true;';
        connStringEl.value = connString;
    };

    const selectAuth = (type) => {
        const windowsRadio = document.getElementById('authWindows');
        const sqlRadio = document.getElementById('authSql');
        const sqlFields = document.getElementById('sqlAuthFields');

        if (!windowsRadio || !sqlRadio || !sqlFields) {
            return;
        }

        if (type === 'windows') {
            windowsRadio.checked = true;
            sqlFields.style.display = 'none';
            windowsRadio.closest('.bg-white')?.classList.add('border-blue-500');
            sqlRadio.closest('.bg-white')?.classList.remove('border-blue-500');
        } else {
            sqlRadio.checked = true;
            sqlFields.style.display = 'grid';
            sqlRadio.closest('.bg-white')?.classList.add('border-blue-500');
            windowsRadio.closest('.bg-white')?.classList.remove('border-blue-500');
        }
        updateConnectionString();
    };

    const setDatabase = (dbName) => {
        const databaseEl = document.getElementById('databaseName');
        if (!databaseEl) {
            return;
        }
        databaseEl.value = dbName;
        updateConnectionString();
    };

    const validateForm = () => {
        const dataSourceKey = document.querySelector('input[name="DataSourceKey"]')?.value.trim() || '';
        const title = document.querySelector('input[name="Title"]')?.value.trim() || '';
        const connString = document.getElementById('connString')?.value.trim() || '';

        if (!dataSourceKey) {
            alert('Anahtar alani zorunludur!');
            return false;
        }

        if (!title) {
            alert('Baslik alani zorunludur!');
            return false;
        }

        if (!connString) {
            alert('Baglanti string olusturulamadi! Lutfen tum alanlari doldurun.');
            return false;
        }

        return true;
    };

    const parseConnectionString = (connString) => {
        const parts = connString.split(';');
        const parsed = {};

        parts.forEach((part) => {
            if (part.trim()) {
                const [key, value] = part.split('=');
                if (key && value) {
                    parsed[key.trim().toLowerCase()] = value.trim();
                }
            }
        });

        return parsed;
    };

    const populateFields = () => {
        const connStringEl = document.getElementById('initialConnString');
        if (!connStringEl) {
            return;
        }
        const connString = connStringEl.value;
        if (!connString) {
            return;
        }

        const parsed = parseConnectionString(connString);

        const serverNameEl = document.getElementById('serverName');
        const databaseNameEl = document.getElementById('databaseName');
        if (serverNameEl) {
            serverNameEl.value = parsed.server || '';
        }
        if (databaseNameEl) {
            databaseNameEl.value = parsed.database || '';
        }

        const authWindows = document.getElementById('authWindows');
        const authSql = document.getElementById('authSql');
        const sqlFields = document.getElementById('sqlAuthFields');
        const username = document.getElementById('username');
        const password = document.getElementById('password');

        if (parsed['integrated security'] === 'true') {
            if (authWindows) {
                authWindows.checked = true;
            }
            if (sqlFields) {
                sqlFields.style.display = 'none';
            }
            authWindows?.closest('.bg-white')?.classList.add('border-blue-500');
            authSql?.closest('.bg-white')?.classList.remove('border-blue-500');
        } else {
            if (authSql) {
                authSql.checked = true;
            }
            if (sqlFields) {
                sqlFields.style.display = 'grid';
            }
            if (username) {
                username.value = parsed['user id'] || 'sa';
            }
            if (password) {
                password.value = parsed.password || '';
            }
            authSql?.closest('.bg-white')?.classList.add('border-blue-500');
            authWindows?.closest('.bg-white')?.classList.remove('border-blue-500');
        }
    };

    const init = () => {
        populateFields();

        document.getElementById('serverName')?.addEventListener('input', updateConnectionString);
        document.getElementById('databaseName')?.addEventListener('input', updateConnectionString);
        document.getElementById('username')?.addEventListener('input', updateConnectionString);
        document.getElementById('password')?.addEventListener('input', updateConnectionString);

        updateConnectionString();
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    window.setDatabase = setDatabase;
    window.selectAuth = selectAuth;
    window.validateForm = validateForm;
})();
