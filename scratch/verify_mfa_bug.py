import urllib.request
import urllib.parse
import http.cookiejar
import ssl
import re

# Disable SSL check
ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE

# Cookie handler
cj = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj), urllib.request.HTTPSHandler(context=ctx))
urllib.request.install_opener(opener)

base_url = "https://localhost:7000"

print("1. Loading Login Page...")
req = urllib.request.Request(f"{base_url}/Identity/Account/Login")
with opener.open(req) as response:
    html = response.read().decode('utf-8')

# Extract Anti-Forgery Token
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

with opener.open(req_post) as response:
    login_html = response.read().decode('utf-8')
    redirect_url = response.url
    print(f"Login Response URL: {redirect_url}")

# Check if we were redirected to LoginWith2fa
if "LoginWith2fa" in redirect_url:
    print("Success: Redirected to 2FA verification page.")
else:
    print("Failed: Was not redirected to 2FA page.")
    # Print error messages in login_html if any
    errors = re.findall(r'<div class="text-danger[^>]*>(.*?)</div>', login_html, re.DOTALL)
    for error in errors:
        print("  Error:", re.sub('<[^<]+?>', '', error).strip())
    # Check if user needs 2FA setup first
    if "TwoFactorAuthentication" in redirect_url:
        print("Note: User is redirected to Two-Factor setup page first.")
    exit(1)

# Now, we are on LoginWith2fa page.
# Let's extract the CSRF token on LoginWith2fa page
mfa_token_match = re.search(r'__RequestVerificationToken" type="hidden" value="([^"]+)"', login_html)
mfa_token = mfa_token_match.group(1) if mfa_token_match else ""
encrypted_otp_match = re.search(r'name="EncryptedOtp" type="hidden" value="([^"]*)"', login_html)
encrypted_otp = encrypted_otp_match.group(1) if encrypted_otp_match else ""

print(f"MFA CSRF Token: {mfa_token}")
print(f"MFA Encrypted OTP length: {len(encrypted_otp)}")
print("All input tags found:")
for match in re.findall(r'<input[^>]+>', login_html):
    print("  ", match)

# 3. Simulate "Resend Code" POST
print("\n3. Triggering 'Resend Code'...")
resend_data = urllib.parse.urlencode({
    "RememberMe": "False",
    "ReturnUrl": "/",
    "__RequestVerificationToken": mfa_token
}).encode('utf-8')

# The resend form posts with handler=ResendCode
resend_req = urllib.request.Request(
    f"{base_url}/Identity/Account/LoginWith2fa?handler=ResendCode",
    data=resend_data,
    headers={"Content-Type": "application/x-www-form-urlencoded"}
)

with opener.open(resend_req) as response:
    resend_html = response.read().decode('utf-8')
    resend_url = response.url
    print(f"Resend Response URL: {resend_url}")

# Find updated CSRF and EncryptedOtp
mfa_token_match = re.search(r'__RequestVerificationToken" type="hidden" value="([^"]+)"', resend_html)
mfa_token = mfa_token_match.group(1) if mfa_token_match else ""
encrypted_otp_match = re.search(r'name="EncryptedOtp" type="hidden" value="([^"]*)"', resend_html)
encrypted_otp = encrypted_otp_match.group(1) if encrypted_otp_match else ""

# 4. Now, submit the main verification form (verify-form) while the current URL in browser has handler=ResendCode!
print("\n4. Submitting code verification form (with handler=ResendCode in address)...")
verify_data = urllib.parse.urlencode({
    "RememberMe": "False",
    "ReturnUrl": "/",
    "EncryptedOtp": encrypted_otp,
    "Input.TwoFactorCode": "123456",
    "__RequestVerificationToken": mfa_token
}).encode('utf-8')

# Find the action attribute of the verify-form in resend_html
form_match = re.search(r'<form[^>]*id="verify-form"[^>]*action="([^"]*)"', resend_html)
form_action = form_match.group(1) if form_match else ""
print(f"Verify Form Action from HTML: {form_action}")

if not form_action:
    submit_url = f"{base_url}/Identity/Account/LoginWith2fa"
elif form_action.startswith("http"):
    submit_url = form_action
else:
    submit_url = f"{base_url}{form_action}"

print(f"Submitting to: {submit_url}")

verify_req = urllib.request.Request(
    submit_url,
    data=verify_data,
    headers={"Content-Type": "application/x-www-form-urlencoded"}
)

with opener.open(verify_req) as response:
    result_html = response.read().decode('utf-8')
    result_url = response.url
    print(f"Final Submission URL: {result_url}")

# Analyze the output HTML to see which handler was executed:
# - If OnPostResendCodeAsync was executed, the output HTML will contain the resend success alert:
#   "A new verification code has been sent to your email."
# - If OnPostAsync was executed, the output HTML will contain the validation failure:
#   "Invalid verification code. Please try again." or model validation warning.

if "A new verification code has been sent" in result_html:
    print("\n[FAIL] BUG STILL PRESENT: The ResendCode handler was triggered instead of code verification!")
elif "Invalid verification code" in result_html:
    print("\n[SUCCESS] BUG FIXED: The code verification handler was correctly triggered!")
else:
    print("\nUnknown status. Searching HTML for clues:")
    # Print warning/alert messages in the response
    for line in result_html.split('\n'):
        if "alert" in line or "text-danger" in line:
            print(f"  Line: {line.strip()}")
