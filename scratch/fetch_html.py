import urllib.request
import json
import ssl

ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE

req = urllib.request.Request("http://localhost:5049/Admin/UserManagement")
# We need to be logged in to access Admin/UserManagement.
# Let's bypass login by hitting an endpoint or just modifying the View temporarily.
