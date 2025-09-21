import os
import requests
import json
from pathlib import Path
from typing import Optional, List, Dict, Any

# Configuration
OLLAMA_API_URL = "http://localhost:11434/api/generate"
MODEL_NAME = "gemma3:12b"
SYSTEM_PROMPT = """You are an experienced systems analyst and technical writer.
Your task is to analyze file contents and create concise yet informative descriptions in English.

For source code files, provide:
1. File type and purpose
2. Main content or functionality
3. List of methods/functions with their signatures and descriptions
4. Key elements (if applicable)
5. Dependencies (if any)

For non-source files, provide:
1. File type and purpose
2. Main content
3. Key elements (if applicable)
4. Dependencies (if any)

Output must be in JSON format with the following structure:
{
  "general_description": "string",
  "methods": [
    {
      "signature": "string",
      "description": "string"
    }
  ]
}
If no methods exist, use "methods": null.
Be concise but informative."""

# Directories to exclude (case sensitive)
EXCLUDE_DIRS = {
    '__pycache__',
    '.git',
    '.idea',
    'venv',
    'node_modules',
    'dist',
    'build',
    'target',
    'bin',
    'obj',
    'cache',
    'temp'
}

# File extensions to treat as source code
SOURCE_CODE_EXTENSIONS = {
    '.py', '.js', '.jsx', '.ts', '.tsx', '.java', '.kt', '.scala',
    '.c', '.cpp', '.h', '.hpp', '.cs', '.go', '.rs', '.php', '.rb',
    '.swift', '.m', '.hs', '.lua', '.pl', '.r', '.sh', '.bash'
}

# File extensions to skip (binary files)
SKIP_EXTENSIONS = {
    '.png', '.jpg', '.jpeg', '.gif', '.bmp', '.tiff', '.ico', '.svg',
    '.pdf', '.zip', '.tar', '.gz', '.7z', '.rar', '.exe', '.dll', '.so',
    '.bin', '.class', '.jar', '.war', '.ear', '.dat', '.ico', '.ttf', '.woff'
}

MAX_FILE_SIZE = 2 * 1024 * 1024  # 2MB

def should_exclude(dirpath: str) -> bool:
    """Check if directory should be excluded from processing"""
    dirname = os.path.basename(dirpath)
    return dirname in EXCLUDE_DIRS or any(
        excluded_dir in dirpath.split(os.sep)
        for excluded_dir in EXCLUDE_DIRS
    )

def get_file_description(file_path: str, file_content: str) -> Optional[Dict[str, Any]]:
    """Get file description from Ollama in JSON format"""
    filename = os.path.basename(file_path)
    ext = os.path.splitext(filename)[1].lower()
    
    # Add file type hint to prompt
    prompt = f"File: {filename}\nPath: {file_path}\n"
    if ext in SOURCE_CODE_EXTENSIONS:
        prompt += "This is a SOURCE CODE file. Analyze methods/functions.\n"
    else:
        prompt += "This is a NON-SOURCE file. Provide general description only.\n"
    
    prompt += f"Content:\n{file_content[:15000]}\n\n"
    prompt += "Output JSON analysis:"

    try:
        response = requests.post(
            OLLAMA_API_URL,
            json={
                "model": MODEL_NAME,
                "system": SYSTEM_PROMPT,
                "prompt": prompt,
                "format": "json",
                "stream": False
            },
            timeout=120
        )
        response.raise_for_status()
        response_data = response.json()
        
        # Handle both direct JSON and response string
        if "response" in response_data:
            try:
                return json.loads(response_data["response"])
            except json.JSONDecodeError:
                print(f"Invalid JSON response for {file_path}")
                return None
        return None
    except Exception as e:
        print(f"Error querying Ollama for file {file_path}: {e}")
        return None

def process_file(file_path: str, output_file: str):
    """Process a single file and save description with methods"""
    # Skip large files
    if os.path.getsize(file_path) > MAX_FILE_SIZE:
        print(f"Skipping large file: {file_path}")
        return
        
    # Skip binary files
    ext = os.path.splitext(file_path)[1].lower()
    if ext in SKIP_EXTENSIONS:
        print(f"Skipping binary file: {file_path}")
        return

    # Read file content
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
    except UnicodeDecodeError:
        try:
            with open(file_path, 'r', encoding='latin-1') as f:
                content = f.read()
        except Exception as e:
            print(f"Could not read file {file_path}: {e}")
            return
    
    # Get description from Ollama
    description = get_file_description(file_path, content)
    if not description:
        return
        
    # Write results to output file
    with open(output_file, 'a', encoding='utf-8') as f:
        f.write(f"File: {file_path}\n")
        f.write(f"General Description:\n{description.get('general_description', 'No description')}\n\n")
        
        methods = description.get('methods')
        if methods and isinstance(methods, list):
            f.write("Methods:\n")
            for i, method in enumerate(methods, 1):
                signature = method.get('signature', 'Unknown signature')
                desc = method.get('description', 'No description')
                f.write(f"{i}. {signature}\n   - {desc}\n")
        f.write("\n" + "="*80 + "\n\n")

def scan_directory(root_dir: str, output_file: str):
    """Recursively scan directory and process files"""
    for dirpath, dirnames, filenames in os.walk(root_dir):
        # Skip excluded directories
        if should_exclude(dirpath):
            print(f"Skipping excluded directory: {dirpath}")
            dirnames[:] = []  # prevent os.walk from descending further
            continue
        
        # Remove excluded directories from dirnames to prevent traversal
        dirnames[:] = [d for d in dirnames if not should_exclude(os.path.join(dirpath, d))]
        
        for filename in filenames:
            file_path = os.path.join(dirpath, filename)
            print(f"Processing file: {file_path}")
            process_file(file_path, output_file)

if __name__ == "__main__":
    import argparse
    
    parser = argparse.ArgumentParser(description='File analyzer with Ollama')
    parser.add_argument('directory', help='Directory to scan')
    parser.add_argument('output', help='Output file for results')
    parser.add_argument('--exclude', nargs='+', default=[], 
                       help='Additional directories to exclude')
    
    args = parser.parse_args()
    
    # Add command line excluded directories to the set
    EXCLUDE_DIRS.update(args.exclude)
    
    # Clear output file before starting
    if os.path.exists(args.output):
        os.remove(args.output)
    
    scan_directory(args.directory, args.output)
    print(f"Analysis complete. Results saved to {args.output}")