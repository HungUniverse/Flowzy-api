"""Read-only workbook metadata for reconciling the authorized group import."""
import json
import re
import openpyxl

source = r"C:/Users/lenovo/Downloads/SU26_EXE101 _ Group List (mentor).xlsx"
book = openpyxl.load_workbook(source, read_only=True, data_only=True)
groups = []
for index, row in enumerate(book.active.values, 1):
    if index == 1 or not row[0]:
        continue
    codes = list(dict.fromkeys(re.findall(r"\bM\d+\b", str(row[17]), re.I)))
    groups.append({"row": index, "groupNo": str(row[0]).strip(),
                   "name": str(row[1]).strip() if row[1] else "EXE101 Group " + str(row[0]).strip(),
                   "mentorCodes": [code.upper() for code in codes]})
print(json.dumps(groups, ensure_ascii=False))
