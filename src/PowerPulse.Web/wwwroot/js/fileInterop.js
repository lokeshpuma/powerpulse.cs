// PowerPulse File and Browser Helpers
window.powerPulseFiles = {
    downloadFile: function (filename, content, contentType) {
        const blob = new Blob([content], { type: contentType || 'text/plain;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    openHtmlInNewTabAndPrint: function (htmlContent) {
        const printWindow = window.open('', '_blank');
        if (printWindow) {
            printWindow.document.write(htmlContent);
            printWindow.document.close();
            printWindow.focus();
            setTimeout(() => {
                printWindow.print();
            }, 300);
        } else {
            alert('Popup blocked. Please allow popups to open the print report.');
        }
    },

    triggerPrint: function () {
        window.print();
    }
};
