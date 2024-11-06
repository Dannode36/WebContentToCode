## Convert any file into a C++ byte array with optional compression
Built for my [DIWiFi](https://github.com/Dannode36/DIWiFi) project where I am hosting webpages on an ESP8266 and was after a more automated solution for converting each `.html`, `.css`, and `.js` file into byte arrays. Currently only supports GZip compression.

## How does it work?
WCTC scans the working directory for files with extension(s) you specify with the `-f` option (defaults to every file), e.g. `-f html css js`.

WCTC will then read in each file as a byte array and apply the compression algorithm specified with the `-e` option (defaults to "none", other option is "gzip").

Then a C++ header-like file will be generated containing `const uint8_t` arrays for each file processed and outputs it to a file name specified with `-o` (defaults to "webFiles.h).

## Anything else?
- `-p` tells WCTC to generate the C++ arrays with a [`PROGMEM`](https://www.arduino.cc/reference/tr/language/variables/utilities/progmem/) macro on them
- `-r` allows WCTC to process the output file (kinda recursive and therefore defaults to not doing to)
