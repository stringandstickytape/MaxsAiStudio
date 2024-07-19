
namespace AiTool3.UI
{
    internal class PlantUMLViewer
    {
        internal static void View(string plantUmlString)
        {
            var form = new Form();
            form.Size = new Size(256, 256);
            form.StartPosition = FormStartPosition.CenterScreen;

            // create a WebView2 that fills the window
            var replHtml = html.Replace("DATAGOESHERE", plantUmlString);
            var wvForm = new WebviewForm(replHtml);
            wvForm.Show();
        }

        private static string html = @"<html><head>
<script>
function loadPakoScript() {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/pako/2.1.0/pako.min.js';
        script.id = 'pako-js';
        script.onload = resolve;
        script.onerror = reject;
        (document.head || document.body).appendChild(script);
    });
}

function encode64(data) {
    let r = """";
    for (let i = 0; i < data.length; i += 3) {
        if (i + 2 == data.length) {
            r += append3bytes(data.charCodeAt(i), data.charCodeAt(i + 1), 0);
        } else if (i + 1 == data.length) {
            r += append3bytes(data.charCodeAt(i), 0, 0);
        } else {
            r += append3bytes(data.charCodeAt(i), data.charCodeAt(i + 1), data.charCodeAt(i + 2));
        }
    }
    return r;
}

function append3bytes(b1, b2, b3) {
    const c1 = b1 >> 2;
    const c2 = ((b1 & 0x3) << 4) | (b2 >> 4);
    const c3 = ((b2 & 0xF) << 2) | (b3 >> 6);
    const c4 = b3 & 0x3F;
    return encode6bit(c1 & 0x3F) + encode6bit(c2 & 0x3F) + encode6bit(c3 & 0x3F) + encode6bit(c4 & 0x3F);
}

function encode6bit(b) {
    if (b < 10) return String.fromCharCode(48 + b);
    b -= 10;
    if (b < 26) return String.fromCharCode(65 + b);
    b -= 26;
    if (b < 26) return String.fromCharCode(97 + b);
    b -= 26;
    if (b == 0) return '-';
    if (b == 1) return '_';
    return '?';
}

function compress(s) {
    // Use pako to deflate the string
    const deflated = pako.deflate(s);
    // Convert the Uint8Array to a string
    return encode64(String.fromCharCode.apply(null, deflated));
}

function generatePlantUmlUrl(plantUmlContent) {
    return loadPakoScript()
        .then(() => {
            const encoded = compress(plantUmlContent);
debugger;
            return `http://www.plantuml.com/plantuml/png/~1${encoded}`;
        });
}


// Usage
const plantUmlContent = `DATAGOESHERE`;

generatePlantUmlUrl(plantUmlContent)
    .then(url => {
        console.log(""PlantUML URL:"", url);
        window.location.href = url; // This will navigate the current window to the URL
    })
    .catch(error => {
        console.error(""Error generating PlantUML URL:"", error);
    });


</script>
";
    }
}