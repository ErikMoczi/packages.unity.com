var fs = require("fs");

if (process.argv.length != 3) {
    console.error("must provide a package name on the command line");
    process.exit(1);
}

var tag = process.env["CI_COMMIT_TAG"] || undefined;

if (tag === undefined) {
    console.error("'CI_COMMIT_TAG *must* be set");
    process.exit(0);
}

var pkgName = process.argv[2];
var jsonPath = "./" + pkgName + "/package.json";

var pkg = JSON.parse(fs.readFileSync(jsonPath));
pkg["version"] = tag;
fs.writeFileSync(jsonPath, JSON.stringify(pkg, null, 4));
