import os
import json
import re
import requests
from typing import List, Dict, Optional, Tuple
import time
from collections import defaultdict

# Настройки Ollama. Переопределяются переменными окружения, чтобы скрипт можно было
# направить на удалённый хост или другую модель без правки кода.
DEFAULT_OLLAMA_BASE_URL = os.environ.get("OLLAMA_BASE_URL", "http://localhost:11434")
DEFAULT_OLLAMA_MODEL = os.environ.get("OLLAMA_MODEL", "gemma3:12b")


class OllamaAnalyzer:
    def __init__(self, base_url: Optional[str] = None):
        self.base_url = (base_url or DEFAULT_OLLAMA_BASE_URL).rstrip("/")
    
    def generate(self, model: str, prompt: str, format: str = "json") -> Optional[Dict]:
        """Отправляет запрос к Ollama API и возвращает JSON ответ"""
        url = f"{self.base_url}/api/generate"
        payload = {
            "model": model,
            "prompt": prompt,
            "format": format,
            "stream": False,
            "options": {
                "temperature": 0.1
            }
        }
        
        try:
            response = requests.post(
                url,
                json=payload,
                headers={"Content-Type": "application/json"},
                timeout=300
            )
            response.raise_for_status()
            return response.json()
        except requests.exceptions.RequestException as e:
            print(f"Ошибка при запросе к Ollama: {str(e)}")
            return None

def _parse_ollama_payload(response: object, context: str) -> Optional[Dict]:
    """Извлекает JSON-объект из ответа Ollama, не прерывая пакетный анализ.

    Модель может вернуть что угодно: не-dict конверт, отсутствующее поле
    "response", невалидный JSON или валидный JSON, который не является объектом
    (список, строка, число). Любой из этих случаев пропускает ОДИН файл и
    возвращает None, вместо того чтобы уронить весь прогон.
    """
    if not isinstance(response, dict):
        print(f"Ollama вернула не объект ({type(response).__name__}) для {context}")
        return None

    payload = response.get("response")
    if payload is None:
        print(f"В ответе Ollama нет поля 'response' для {context}")
        return None

    if isinstance(payload, dict):
        return dict(payload)

    if not isinstance(payload, str):
        print(f"Поле 'response' имеет тип {type(payload).__name__} для {context}")
        return None

    try:
        result = json.loads(payload)
    except json.JSONDecodeError:
        print(f"Неверный JSON ответ от Ollama для {context}")
        return None

    if not isinstance(result, dict):
        print(f"Ollama вернула JSON типа {type(result).__name__}, ожидался объект, для {context}")
        return None

    return result


def find_csharp_files(directory: str) -> Tuple[List[str], List[Tuple[str, str]]]:
    """Находит все файлы .cs и пары ByDoubles/ByUnits"""
    all_files = []
    potential_pairs = defaultdict(dict)
    pattern = re.compile(r"(.*)(ByDoubles|ByUnits)(.*)\.cs$")
    
    for root, _, files in os.walk(directory):
        for file in files:
            file_path = os.path.join(root, file)
            all_files.append(file_path)
            
            # Ищем файлы для попарного анализа
            match = pattern.search(file)
            if match:
                base_name = match.group(1) + match.group(3)
                pair_type = match.group(2)
                potential_pairs[base_name][pair_type] = file_path
    
    # Формируем пары файлов
    file_pairs = []
    for base_name, files in potential_pairs.items():
        if "ByDoubles" in files and "ByUnits" in files:
            file_pairs.append((files["ByDoubles"], files["ByUnits"]))
    
    # Файлы без пар
    single_files = [
        f for f in all_files 
        if not any(f in pair for pair in file_pairs)
    ]
    
    return single_files, file_pairs

def analyze_single_file(analyzer: OllamaAnalyzer, file_path: str, model: str) -> Optional[Dict]:
    """Анализирует отдельный файл на наличие несопоставленных методов"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
    except Exception as e:
        print(f"Ошибка чтения файла {file_path}: {str(e)}")
        return None

    prompt = f"""
Проанализируй этот файл C# и определи, всем ли методам с UnitsNet типами 
соответствуют эквивалентные версии с double в этом же файле.

Ответ предоставь в JSON формате:
{{
    "file": "имя_файла",
    "problems": [
        {{
            "method": "имя_метода",
            "issue": "описание_проблемы",
            "unitsnet_version": "сигнатура",
            "double_version": "сигнатура (если есть)"
        }}
    ],
    "status": "ok|error"
}}

Файл: {file_path}
Код:
{content}
"""

    response = analyzer.generate(model, prompt)
    if not response:
        return None

    result = _parse_ollama_payload(response, f"файла {file_path}")
    if result is None:
        return None

    result["file"] = file_path
    return result

def analyze_file_pair(analyzer: OllamaAnalyzer, file1: str, file2: str, model: str) -> Optional[Dict]:
    """Сравнивает пару файлов ByDoubles/ByUnits"""
    try:
        with open(file1, 'r', encoding='utf-8') as f1, open(file2, 'r', encoding='utf-8') as f2:
            content1 = f1.read()
            content2 = f2.read()
    except Exception as e:
        print(f"Ошибка чтения файлов: {str(e)}")
        return None

    prompt = f"""
Сравни эти два файла C# и определи, являются ли они семантически эквивалентными,
учитывая что один использует UnitsNet типы, а другой - double.

Файл 1 ({os.path.basename(file1)}):
{content1}

Файл 2 ({os.path.basename(file2)}):
{content2}

Ответ предоставь в JSON формате:
{{
    "file1": "имя_файла1",
    "file2": "имя_файла2",
    "equivalent": true|false,
    "differences": [
        {{
            "description": "описание_различия",
            "details": "подробности"
        }}
    ],
    "missing_methods": [
        {{
            "method": "имя_метода",
            "in_file": "в_каком_файле_отсутствует"
        }}
    ],
    "status": "ok|error"
}}
"""

    response = analyzer.generate(model, prompt)
    if not response:
        return None

    result = _parse_ollama_payload(response, f"пары {file1} и {file2}")
    if result is None:
        return None

    result["file1"] = file1
    result["file2"] = file2
    return result

def analyze_directory(directory: str, model: Optional[str] = None, base_url: Optional[str] = None):
    """Анализирует все файлы в директории"""
    model = model or DEFAULT_OLLAMA_MODEL
    analyzer = OllamaAnalyzer(base_url)
    single_files, file_pairs = find_csharp_files(directory)
    results = {
        "single_files": [],
        "file_pairs": []
    }
    
    print(f"Начинаем анализ {len(single_files)} отдельных файлов и {len(file_pairs)} пар файлов...")
    
    # Анализ отдельных файлов
    for i, file_path in enumerate(single_files, 1):
        print(f"\n[Файл {i}/{len(single_files)}] Анализ {os.path.basename(file_path)}")
        
        start_time = time.time()
        analysis = analyze_single_file(analyzer, file_path, model)
        elapsed = time.time() - start_time
        
        if analysis:
            analysis["analysis_time"] = round(elapsed, 2)
            results["single_files"].append(analysis)
            
            problems = analysis.get("problems")
            if problems:
                print("Найдены проблемы:")
                for problem in problems if isinstance(problems, list) else []:
                    if not isinstance(problem, dict):
                        print(f" - (нераспознанная запись: {problem!r})")
                        continue
                    method = problem.get("method", "<без имени>")
                    issue = problem.get("issue", "<без описания>")
                    print(f" - {method}: {issue}")
            else:
                print("Проблем не обнаружено")
        
        time.sleep(1)
    
    # Анализ пар файлов
    for i, (file1, file2) in enumerate(file_pairs, 1):
        print(f"\n[Пара {i}/{len(file_pairs)}] Сравниваю:")
        print(f" - {os.path.basename(file1)}")
        print(f" - {os.path.basename(file2)}")
        
        start_time = time.time()
        comparison = analyze_file_pair(analyzer, file1, file2, model)
        elapsed = time.time() - start_time
        
        if comparison:
            comparison["analysis_time"] = round(elapsed, 2)
            results["file_pairs"].append(comparison)
            
            if not comparison.get("equivalent", True):
                print("Найдены различия:")
                differences = comparison.get("differences") or []
                for diff in differences if isinstance(differences, list) else []:
                    if isinstance(diff, dict):
                        print(f" - {diff.get('description', '<без описания>')}")
                    else:
                        print(f" - (нераспознанная запись: {diff!r})")

                missing_methods = comparison.get("missing_methods") or []
                for missing in missing_methods if isinstance(missing_methods, list) else []:
                    if isinstance(missing, dict):
                        print(f" - Отсутствует метод {missing.get('method', '<без имени>')} "
                              f"в {missing.get('in_file', '<неизвестный файл>')}")
                    else:
                        print(f" - (нераспознанная запись: {missing!r})")
            else:
                print("Файлы семантически эквивалентны")
        
        time.sleep(1)
    
    # Сохраняем полный отчет
    report = {
        "timestamp": time.strftime("%Y-%m-%d %H:%M:%S"),
        "model_used": model,
        "results": results
    }
    
    report_file = f"combined_analysis_report_{int(time.time())}.json"
    with open(report_file, 'w', encoding='utf-8') as f:
        json.dump(report, f, indent=2, ensure_ascii=False)
    
    print(f"\nАнализ завершен. Отчет сохранен в {report_file}")
    return report

if __name__ == "__main__":
    import argparse
    
    parser = argparse.ArgumentParser(
        description="Комбинированный анализ файлов C# (отдельные файлы и пары ByDoubles/ByUnits)"
    )
    parser.add_argument("directory", help="Директория с файлами .cs для анализа")
    parser.add_argument(
        "--model",
        default=DEFAULT_OLLAMA_MODEL,
        help="Модель Ollama (переменная окружения OLLAMA_MODEL; "
             "по умолчанию: %(default)s)"
    )
    parser.add_argument(
        "--base-url",
        default=DEFAULT_OLLAMA_BASE_URL,
        help="Базовый URL Ollama API (переменная окружения OLLAMA_BASE_URL; "
             "по умолчанию: %(default)s)"
    )

    args = parser.parse_args()

    if not os.path.isdir(args.directory):
        print(f"Ошибка: {args.directory} не является директорией")
        exit(1)

    analyze_directory(args.directory, args.model, args.base_url)