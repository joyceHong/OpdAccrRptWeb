const fs = require("node:fs");
const path = require("node:path");

const destinationDirectory = path.join(__dirname, "..", "wwwroot", "vendor");

fs.mkdirSync(destinationDirectory, { recursive: true });

const vendorFiles = [
    {
        source: path.join(__dirname, "..", "node_modules", "vue", "dist", "vue.global.prod.js"),
        destination: path.join(destinationDirectory, "vue.global.prod.js")
    },
    {
        source: path.join(__dirname, "..", "node_modules", "vue-router", "dist", "vue-router.global.prod.js"),
        destination: path.join(destinationDirectory, "vue-router.global.prod.js")
    }
];

for (const vendorFile of vendorFiles) {
    fs.copyFileSync(vendorFile.source, vendorFile.destination);
    console.log(`Copied vendor runtime to ${vendorFile.destination}`);
}
