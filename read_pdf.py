import pypdf
import os

pdf_files = [
    "IT15_PROJECT_TITLE_PROPOSAL-V4_1.pdf",
    "IT15_ERD_DATADICTIONARY-V3.pdf"
]

output_file = "pdf_content.txt"

with open(output_file, "w", encoding="utf-8") as f:
    for pdf_path in pdf_files:
        f.write(f"\n--- Processing {pdf_path} ---\n")
        if not os.path.exists(pdf_path):
            f.write(f"File not found: {pdf_path}\n")
            continue
            
        try:
            reader = pypdf.PdfReader(pdf_path)
            f.write(f"Pages: {len(reader.pages)}\n")
            
            text = ""
            for page in reader.pages:
                text += page.extract_text() + "\n"
            
            f.write(text)
            f.write("\n" + "="*50 + "\n")

        except Exception as e:
            f.write(f"Error reading {pdf_path}: {e}\n")

print(f"Extracted content to {output_file}")
