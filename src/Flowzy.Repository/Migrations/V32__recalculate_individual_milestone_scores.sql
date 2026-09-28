-- Store the individual milestone score before applying the milestone weight.
-- Final weighted totals remain calculated at read/export time.
UPDATE milestone_member_scores scores
SET calculated_score = CAST(
        grades.score * scores.contribution_percent / 100.00
        AS DECIMAL(7, 4)
    )
FROM milestone_group_grades grades
WHERE scores.group_grade_id = grades.id;
