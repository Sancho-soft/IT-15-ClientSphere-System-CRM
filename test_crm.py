import urllib.request
import urllib.parse
import http.cookiejar
import ssl
import re

# Ignore SSL certificate verification
ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE

# Enable cookie handling
cj = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj), urllib.request.HTTPSHandler(context=ctx))
urllib.request.install_opener(opener)

base_url = "https://localhost:7000"

print("0. Requesting Home Page...")
try:
    with opener.open(urllib.request.Request(f"{base_url}/")) as response:
        print(f"Home GET Status: {response.status}")
except Exception as e:
    print(f"Home GET Failed: {e}")

print("1. Requesting Login Page...")
req = urllib.request.Request(f"{base_url}/Identity/Account/Login")
try:
    with opener.open(req) as response:
        html = response.read().decode('utf-8')
        print(f"Login GET Status: {response.status}")
except Exception as e:
    print(f"Login GET Failed: {e}")
    exit(1)

# Extract Verification Token
token_match = re.search(r'__RequestVerificationToken" type="hidden" value="([^"]+)"', html)
token = token_match.group(1) if token_match else ""
print(f"CSRF Token: {token}")

print("\n2. Logging in as SuperAdmin...")
login_data = urllib.parse.urlencode({
    "Input.Email": "superadmin@clientsphere.com",
    "Input.Password": "DefaultSeed@123!",
    "__RequestVerificationToken": token,
    "cf-turnstile-response": "bypass_token"
}).encode('utf-8')

req_post = urllib.request.Request(
    f"{base_url}/Identity/Account/Login", 
    data=login_data,
    headers={"Content-Type": "application/x-www-form-urlencoded"}
)

try:
    with opener.open(req_post) as response:
        print(f"Login POST Status: {response.status}")
        print(f"Redirected URL: {response.url}")
        body = response.read().decode('utf-8')
        # Print lines with validation-summary or validation errors
        for line in body.split('\n'):
            if "validation-summary-errors" in line or "text-danger" in line or "alert" in line:
                print(f"Validation error line: {line.strip()}")
except Exception as e:
    print(f"Login POST Failed: {e}")

# Check cookies
print("\nCookies:")
for cookie in cj:
    print(f"  {cookie.name} = {cookie.value}")

def fetch_page(name, path):
    print(f"\nRequesting {name} Page ({path})...")
    req = urllib.request.Request(f"{base_url}{path}")
    req.add_header("Bypass-MFA", "true")
    try:
        with opener.open(req) as response:
            print(f"{name} Page Status: {response.status}")
            print(f"{name} Page Final URL: {response.url}")
            body = response.read().decode('utf-8')
            # Look for common error phrases or headers
            if "error" in body.lower() or "exception" in body.lower() or "denied" in body.lower() or "forbidden" in body.lower():
                print(f"Potential error/denied/forbidden in {name} body!")
                # Print a slice around the first occurrence of "error" or "denied" or "forbidden"
                for word in ["error", "exception", "denied", "forbidden"]:
                    idx = body.lower().find(word)
                    if idx != -1:
                        print(f"Context for '{word}': ... {body[max(0, idx-100):min(len(body), idx+200)]} ...")
                        break
    except Exception as e:
        print(f"{name} Page Failed: {e}")
        if hasattr(e, 'read'):
            print(f"Error body: {e.read().decode('utf-8')}")

fetch_page("Billing", "/Billing")
fetch_page("Sales", "/Sales")
fetch_page("Customers", "/Customers")
