// PowerPulse IndexedDB Storage Module
const DB_NAME = 'PowerPulseDB';
const DB_VERSION = 1;

window.powerPulseDb = {
    db: null,

    init: function () {
        return new Promise((resolve, reject) => {
            if (this.db) {
                resolve();
                return;
            }

            const request = indexedDB.open(DB_NAME, DB_VERSION);

            request.onupgradeneeded = (event) => {
                const db = event.target.result;

                if (!db.objectStoreNames.contains('profile')) {
                    db.createObjectStore('profile', { keyPath: 'id' });
                }
                if (!db.objectStoreNames.contains('settings')) {
                    db.createObjectStore('settings', { keyPath: 'key' });
                }
                if (!db.objectStoreNames.contains('tariff')) {
                    db.createObjectStore('tariff', { keyPath: 'id' });
                }
                if (!db.objectStoreNames.contains('carbon')) {
                    db.createObjectStore('carbon', { keyPath: 'id' });
                }
                if (!db.objectStoreNames.contains('appliances')) {
                    db.createObjectStore('appliances', { keyPath: 'id' });
                }
                if (!db.objectStoreNames.contains('datasets')) {
                    db.createObjectStore('datasets', { keyPath: 'id' });
                }
                if (!db.objectStoreNames.contains('readings')) {
                    const store = db.createObjectStore('readings', { keyPath: 'id' });
                    store.createIndex('datasetId', 'datasetId', { unique: false });
                    store.createIndex('timestamp', 'timestamp', { unique: false });
                }
                if (!db.objectStoreNames.contains('recommendations')) {
                    db.createObjectStore('recommendations', { keyPath: 'id' });
                }
            };

            request.onsuccess = (event) => {
                this.db = event.target.result;
                resolve();
            };

            request.onerror = (event) => {
                console.error('IndexedDB open error:', event.target.error);
                reject(event.target.error);
            };
        });
    },

    getProfile: async function () {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('profile', 'readonly');
            const store = tx.objectStore('profile');
            const req = store.getAll();
            req.onsuccess = () => resolve(req.result && req.result.length > 0 ? req.result[0] : null);
            req.onerror = () => resolve(null);
        });
    },

    saveProfile: async function (profile) {
        await this.init();
        return new Promise((resolve, reject) => {
            const tx = this.db.transaction('profile', 'readwrite');
            const store = tx.objectStore('profile');
            store.clear();
            const req = store.put(profile);
            req.onsuccess = () => resolve();
            req.onerror = (e) => reject(e.target.error);
        });
    },

    getSettings: async function () {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('settings', 'readonly');
            const store = tx.objectStore('settings');
            const req = store.get('app_settings');
            req.onsuccess = () => resolve(req.result ? req.result.value : null);
            req.onerror = () => resolve(null);
        });
    },

    saveSettings: async function (settings) {
        await this.init();
        return new Promise((resolve, reject) => {
            const tx = this.db.transaction('settings', 'readwrite');
            const store = tx.objectStore('settings');
            const req = store.put({ key: 'app_settings', value: settings });
            req.onsuccess = () => resolve();
            req.onerror = (e) => reject(e.target.error);
        });
    },

    getTariff: async function () {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('tariff', 'readonly');
            const store = tx.objectStore('tariff');
            const req = store.getAll();
            req.onsuccess = () => resolve(req.result && req.result.length > 0 ? req.result[0] : null);
            req.onerror = () => resolve(null);
        });
    },

    saveTariff: async function (tariff) {
        await this.init();
        return new Promise((resolve, reject) => {
            const tx = this.db.transaction('tariff', 'readwrite');
            const store = tx.objectStore('tariff');
            store.clear();
            const req = store.put(tariff);
            req.onsuccess = () => resolve();
            req.onerror = (e) => reject(e.target.error);
        });
    },

    getCarbonFactor: async function () {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('carbon', 'readonly');
            const store = tx.objectStore('carbon');
            const req = store.getAll();
            req.onsuccess = () => resolve(req.result && req.result.length > 0 ? req.result[0] : null);
            req.onerror = () => resolve(null);
        });
    },

    saveCarbonFactor: async function (factor) {
        await this.init();
        return new Promise((resolve, reject) => {
            const tx = this.db.transaction('carbon', 'readwrite');
            const store = tx.objectStore('carbon');
            store.clear();
            const req = store.put(factor);
            req.onsuccess = () => resolve();
            req.onerror = (e) => reject(e.target.error);
        });
    },

    getAppliances: async function () {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('appliances', 'readonly');
            const store = tx.objectStore('appliances');
            const req = store.getAll();
            req.onsuccess = () => resolve(req.result || []);
            req.onerror = () => resolve([]);
        });
    },

    saveAppliance: async function (appliance) {
        await this.init();
        return new Promise((resolve, reject) => {
            const tx = this.db.transaction('appliances', 'readwrite');
            const store = tx.objectStore('appliances');
            const req = store.put(appliance);
            req.onsuccess = () => resolve();
            req.onerror = (e) => reject(e.target.error);
        });
    },

    deleteAppliance: async function (id) {
        await this.init();
        return new Promise((resolve, reject) => {
            const tx = this.db.transaction('appliances', 'readwrite');
            const store = tx.objectStore('appliances');
            const req = store.delete(id);
            req.onsuccess = () => resolve();
            req.onerror = (e) => reject(e.target.error);
        });
    },

    clearAppliances: async function () {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('appliances', 'readwrite');
            tx.objectStore('appliances').clear();
            tx.oncomplete = () => resolve();
        });
    },

    getDatasets: async function () {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('datasets', 'readonly');
            const req = tx.objectStore('datasets').getAll();
            req.onsuccess = () => resolve(req.result || []);
            req.onerror = () => resolve([]);
        });
    },

    saveDataset: async function (dataset) {
        await this.init();
        return new Promise((resolve, reject) => {
            const tx = this.db.transaction('datasets', 'readwrite');
            const req = tx.objectStore('datasets').put(dataset);
            req.onsuccess = () => resolve();
            req.onerror = (e) => reject(e.target.error);
        });
    },

    deleteDataset: async function (id) {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('datasets', 'readwrite');
            tx.objectStore('datasets').delete(id);
            tx.oncomplete = () => resolve();
        });
    },

    clearDatasets: async function () {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('datasets', 'readwrite');
            tx.objectStore('datasets').clear();
            tx.oncomplete = () => resolve();
        });
    },

    getAllReadings: async function () {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('readings', 'readonly');
            const req = tx.objectStore('readings').getAll();
            req.onsuccess = () => resolve(req.result || []);
            req.onerror = () => resolve([]);
        });
    },

    saveReadingsBatch: async function (readings) {
        await this.init();
        return new Promise((resolve, reject) => {
            const tx = this.db.transaction('readings', 'readwrite');
            const store = tx.objectStore('readings');
            for (let i = 0; i < readings.length; i++) {
                store.put(readings[i]);
            }
            tx.oncomplete = () => resolve();
            tx.onerror = (e) => reject(e.target.error);
        });
    },

    deleteReadingsByDataset: async function (datasetId) {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('readings', 'readwrite');
            const store = tx.objectStore('readings');
            const index = store.index('datasetId');
            const req = index.openKeyCursor(IDBKeyRange.only(datasetId));
            req.onsuccess = (event) => {
                const cursor = event.target.result;
                if (cursor) {
                    store.delete(cursor.primaryKey);
                    cursor.continue();
                }
            };
            tx.oncomplete = () => resolve();
        });
    },

    clearReadings: async function () {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('readings', 'readwrite');
            tx.objectStore('readings').clear();
            tx.oncomplete = () => resolve();
        });
    },

    getRecommendations: async function () {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('recommendations', 'readonly');
            const req = tx.objectStore('recommendations').getAll();
            req.onsuccess = () => resolve(req.result || []);
            req.onerror = () => resolve([]);
        });
    },

    saveRecommendations: async function (recs) {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('recommendations', 'readwrite');
            const store = tx.objectStore('recommendations');
            store.clear();
            for (let i = 0; i < recs.length; i++) {
                store.put(recs[i]);
            }
            tx.oncomplete = () => resolve();
        });
    },

    clearRecommendations: async function () {
        await this.init();
        return new Promise((resolve) => {
            const tx = this.db.transaction('recommendations', 'readwrite');
            tx.objectStore('recommendations').clear();
            tx.oncomplete = () => resolve();
        });
    },

    clearAll: async function () {
        await this.init();
        return new Promise((resolve) => {
            const storeNames = ['profile', 'settings', 'tariff', 'carbon', 'appliances', 'datasets', 'readings', 'recommendations'];
            const tx = this.db.transaction(storeNames, 'readwrite');
            for (const name of storeNames) {
                tx.objectStore(name).clear();
            }
            tx.oncomplete = () => resolve();
        });
    }
};
