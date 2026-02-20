import zipfile
import os

def list_media(docx_path):
    try:
        with zipfile.ZipFile(docx_path) as z:
            # List all files in the zip
            all_files = z.namelist()
            # Filter for media directory
            media_files = [f for f in all_files if f.startswith('word/media/')]
            return media_files
    except Exception as e:
        return [f"Error: {e}"]

if __name__ == "__main__":
    files = ["rockyfile.docx", "louiefile.docx"]
    for f in files:
        if os.path.exists(f):
            print(f"--- MEDIA IN {f} ---")
            media = list_media(f)
            if media:
                for m in media:
                    print(m)
            else:
                print("No media files found.")
            print("\n------------------------\n")
        else:
            print(f"File not found: {f}")
