"""Optional end-to-end smoke test. Requires Chromium and Python websockets."""
import asyncio
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import tempfile
import time
import urllib.request

import websockets

ROOT = Path(__file__).resolve().parents[1]
DOTNET = shutil.which("dotnet") or str(Path.home() / ".dotnet" / "dotnet")
CHROME = shutil.which("chromium") or shutil.which("chromium-browser")
if not CHROME:
    raise SystemExit("Chromium is required for this optional smoke test.")


def free_port():
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


async def main():
    app_port, debug_port = free_port(), free_port()
    with tempfile.TemporaryDirectory(prefix="gameshelf-browser-") as temporary:
        env = os.environ.copy()
        env["GAMESHELF_DATA_FILE"] = str(Path(temporary) / "games.json")
        server = subprocess.Popen([
            DOTNET, "run", "--project", str(ROOT / "src/GameShelf.Web/GameShelf.Web.csproj"),
            "--no-build", "--urls", f"http://127.0.0.1:{app_port}"
        ], cwd=ROOT, env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        chrome = None
        try:
            for _ in range(100):
                try:
                    urllib.request.urlopen(f"http://127.0.0.1:{app_port}/library", timeout=1).close()
                    break
                except Exception:
                    await asyncio.sleep(.1)
            else:
                raise RuntimeError("GameShelf server did not start. Build the solution first.")

            chrome = subprocess.Popen([
                CHROME, "--headless=new", "--no-sandbox", "--disable-gpu",
                "--disable-dev-shm-usage", "--disable-crash-reporter",
                "--remote-allow-origins=*", f"--remote-debugging-port={debug_port}",
                f"--user-data-dir={Path(temporary) / 'chrome'}",
                f"http://127.0.0.1:{app_port}/library"
            ], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            for _ in range(100):
                try:
                    pages = json.load(urllib.request.urlopen(f"http://127.0.0.1:{debug_port}/json", timeout=1))
                    page = next(p for p in pages if p["type"] == "page" and "/library" in p["url"])
                    break
                except Exception:
                    await asyncio.sleep(.1)
            else:
                raise RuntimeError("Chromium debugging endpoint did not start.")

            async with websockets.connect(page["webSocketDebuggerUrl"], max_size=4_000_000) as ws:
                next_id = 0

                async def evaluate(expression):
                    nonlocal next_id
                    next_id += 1
                    command_id = next_id
                    await ws.send(json.dumps({
                        "id": command_id, "method": "Runtime.evaluate",
                        "params": {"expression": expression, "returnByValue": True, "awaitPromise": True}
                    }))
                    while True:
                        message = json.loads(await ws.recv())
                        if message.get("id") == command_id:
                            result = message.get("result", {})
                            if "exceptionDetails" in result:
                                raise RuntimeError(result["exceptionDetails"])
                            return result.get("result", {}).get("value")

                async def wait_for(expression, timeout=10):
                    deadline = time.monotonic() + timeout
                    while time.monotonic() < deadline:
                        if await evaluate(expression):
                            return
                        await asyncio.sleep(.15)
                    raise RuntimeError("Timed out waiting for " + expression)

                await wait_for("document.body?.innerText.includes('My library')")
                await asyncio.sleep(1)
                await evaluate("Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('Add a game')).click()")
                await wait_for("!!document.querySelector('.modal #editor-title')")
                await evaluate("""(() => {
                    const title = document.querySelector('input[placeholder="e.g. Hollow Knight"]');
                    title.value = 'Browser Smoke Game'; title.dispatchEvent(new Event('change', {bubbles:true}));
                    const genre = document.querySelector('input[placeholder="e.g. Adventure"]');
                    genre.value = 'Puzzle'; genre.dispatchEvent(new Event('change', {bubbles:true}));
                    return true;
                })()""")
                await asyncio.sleep(.6)
                await evaluate("document.querySelector('.modal form').requestSubmit()")
                await wait_for("Array.from(document.querySelectorAll('.game-card')).some(c => c.innerText.includes('Browser Smoke Game')) && !document.querySelector('.modal')")
                print("PASS: add game through Blazor form")

                await evaluate("document.querySelector('button[aria-label=\"View details for Browser Smoke Game\"]').click()")
                await wait_for("document.querySelector('#details-title')?.innerText === 'Browser Smoke Game'")
                print("PASS: view game details")

                await evaluate("Array.from(document.querySelectorAll('.details-modal button')).find(b => b.textContent === 'Edit game').click()")
                await wait_for("!!document.querySelector('.game-form')")
                await evaluate("""(() => {
                    const form = document.querySelector('.game-form');
                    const status = form.querySelector('select'); status.value='Playing'; status.dispatchEvent(new Event('change',{bubbles:true}));
                    const score = form.querySelector('input[min="1"]'); score.value='8'; score.dispatchEvent(new Event('change',{bubbles:true}));
                    const hours = form.querySelector('input[min="0"]'); hours.value='3'; hours.dispatchEvent(new Event('change',{bubbles:true}));
                    const notes = form.querySelector('textarea'); notes.value='Browser smoke note.'; notes.dispatchEvent(new Event('change',{bubbles:true}));
                    return true;
                })()""")
                await asyncio.sleep(.6)
                await evaluate("document.querySelector('.game-form').requestSubmit()")
                await wait_for("!document.querySelector('.game-form') && Array.from(document.querySelectorAll('.game-card')).some(c => c.innerText.includes('Browser Smoke Game') && c.innerText.includes('Playing') && c.innerText.includes('8/10'))")
                await evaluate("document.querySelector('button[aria-label=\"View details for Browser Smoke Game\"]').click()")
                await wait_for("document.querySelector('.details-notes')?.innerText.includes('Browser smoke note.')")
                await evaluate("Array.from(document.querySelectorAll('.details-modal button')).find(b => b.textContent === 'Close').click()")
                await wait_for("!document.querySelector('.details-modal')")
                print("PASS: edit game and persist details")

                await evaluate("""Array.from(document.querySelectorAll('.game-card')).find(c => c.innerText.includes('Browser Smoke Game')).querySelector('button.favorite-button').click()""")
                await wait_for("!!Array.from(document.querySelectorAll('.game-card')).find(c => c.innerText.includes('Browser Smoke Game'))?.querySelector('.favorite-button.is-favorite')")
                print("PASS: favorite game")

                await evaluate("""(() => { const search=document.querySelector('input[type=search]'); search.value='Browser Smoke Game'; search.dispatchEvent(new Event('input',{bubbles:true})); return true; })()""")
                await wait_for("document.querySelectorAll('.game-card').length === 1")
                print("PASS: search filters games")

                await evaluate("""Array.from(document.querySelectorAll('.game-card')).find(c => c.innerText.includes('Browser Smoke Game')).querySelector('button[aria-label^="Delete"]').click()""")
                await wait_for("!!document.querySelector('#delete-title')")
                await evaluate("Array.from(document.querySelectorAll('.confirm-modal button')).find(b => b.textContent.includes('Delete game')).click()")
                await wait_for("!Array.from(document.querySelectorAll('.game-card')).some(c => c.innerText.includes('Browser Smoke Game'))")
                print("PASS: delete game through confirmation")
        finally:
            if chrome:
                chrome.terminate()
                try:
                    chrome.wait(timeout=4)
                except subprocess.TimeoutExpired:
                    chrome.kill()
            server.terminate()
            try:
                server.wait(timeout=4)
            except subprocess.TimeoutExpired:
                server.kill()


asyncio.run(main())
