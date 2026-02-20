import zipfile
import xml.etree.ElementTree as ET
import os

def extract_text(docx_path):
    try:
        with zipfile.ZipFile(docx_path) as z:
            xml_content = z.read('word/document.xml')
            tree = ET.fromstring(xml_content)
            
            # recursive search for all text nodes
            text_parts = []
            for node in tree.iter():
                # Check if the tag name ends with 't' (text)
                # Word XML tags are like {http://schemas.openxmlformats.org/wordprocessingml/2006/main}t
                if node.tag.endswith('}t'):
                    if node.text:
                        text_parts.append(node.text)
                    else:
                        text_parts.append(" ") 
                elif node.tag.endswith('}tab'):
                    text_parts.append("\t")
                elif node.tag.endswith('}br') or node.tag.endswith('}cr'):
                    text_parts.append("\n")
                elif node.tag.endswith('}p'):
                    text_parts.append("\n")
            
            return "".join(text_parts)
    except Exception as e:
        return f"Error: {e}"

if __name__ == "__main__":
    files = ["rockyfile.docx", "louiefile.docx"]
    for f in files:
        if os.path.exists(f):
            print(f"--- CONTENT OF {f} ---")
            content = extract_text(f)
            # Basic cleanup
            lines = [line.strip() for line in content.split('\n') if line.strip()]
            print("\n".join(lines))
            print("\n------------------------\n")
        else:
            print(f"File not found: {f}")
