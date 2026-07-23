export function getAcademicYearStart() {
    const now = new Date();
    const academicYear = now.getMonth() >= 4
        ? now.getFullYear()
        : now.getFullYear() - 1;

    return new Date(academicYear, 8, 1);
}

export function getValidChildDob() {
    const academicYearStart = getAcademicYearStart();

    return {
        day: '02',
        month: '09',
        year: (academicYearStart.getFullYear() - 19).toString()
    };
}

export function getInvalidChildDob() {
    const academicYearStart = getAcademicYearStart();

    return {
        day: '01',
        month: '09',
        year: (academicYearStart.getFullYear() - 19).toString()
    };
}