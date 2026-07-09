const activeIndex = {};
const resultCache = {};
function searchSchool(query, index) {
    var id = `ChildList[${index}].school.Name`
    if (query.length >= 3 && query !== null) {
        // Updated to use new SearchSchools endpoint
        fetch('/Check/SearchSchools?query=' + encodeURIComponent(query))
            .then(response => {
                if (!response.ok) {
                    throw new Error('Search failed');
                }
                return response.json();
            })
            .then(data => {
                const list = document.getElementById(`schoolList${index}`);
                list.innerHTML = '';
                resultCache[index] = data;
                activeIndex[index] = -1;

                data.forEach((school, i) => {
                    const li = document.createElement('li');

                    li.id = `school-${index}-${i}`;
                    li.setAttribute('role', 'option');
                    li.setAttribute('aria-selected', 'false');

                    li.className = (i % 2 === 0)
                        ? 'autocomplete__option'
                        : 'autocomplete__option autocomplete__option--odd';

                    li.textContent = `${school.name}, ${school.id}, ${school.postcode}, ${school.la}`;

                    li.addEventListener('click', () => {
                        selectSchool(school.name, school.id, school.la, school.postcode, index);
                    });

                    list.appendChild(li);
                });

                openList(index);
            })
            .catch(error => {
                console.error('Error searching schools:', error);
                document.getElementById(`schoolList${index}`).innerHTML = '<li class="autocomplete__option">Error searching schools</li>';
            });
    } else {
        document.getElementById(`schoolList${index}`).innerHTML = '';
    }
}

function selectSchool(school, urn, la, postcode, index) {
    // element_ids
    var schoolName = `ChildList[${index}].School.Name`;
    var schoolURN = `ChildList[${index}].School.URN`;
    var schoolPostcode = `ChildList[${index}].School.Postcode`;
    var schoolLA = `ChildList[${index}].School.LA`;
    var schoolSearch = `ChildList[${index}].School`
    // set values
    document.getElementById(schoolName).value = school;
    document.getElementById(schoolURN).value = urn;
    document.getElementById(schoolPostcode).value = postcode;
    document.getElementById(schoolLA).value = la;
    document.getElementById(schoolSearch).value = `${school}, ${urn}, ${postcode}, ${la}`;
    // set in local storage
    localStorage.setItem(`schoolName${index}`, school);
    localStorage.setItem(`schoolURN${index}`, urn);
    localStorage.setItem(`schoolPostcode${index}`, postcode);
    localStorage.setItem(`schoolLA${index}`, la);
    // clear options
    document.getElementById(`schoolList${index}`).innerHTML = '';
}

function openList(index) {
    const input = document.getElementById(`ChildList[${index}].School`);
    const list = document.getElementById(`schoolList${index}`);

    list.hidden = false;
    input.setAttribute('aria-expanded', 'true');
}

function closeList(index) {
    const input = document.getElementById(`ChildList[${index}].School`);
    const list = document.getElementById(`schoolList${index}`);

    list.hidden = true;
    list.innerHTML = '';
    input.setAttribute('aria-expanded', 'false');
    input.removeAttribute('aria-activedescendant');

    activeIndex[index] = -1;
}

function setActive(index, optionIndex) {
    const list = document.getElementById(`schoolList${index}`);
    const options = list.querySelectorAll('[role="option"]');

    options.forEach(o => o.setAttribute('aria-selected', 'false'));

    const option = options[optionIndex];
    if (!option) return;

    option.setAttribute('aria-selected', 'true');

    document
        .getElementById(`ChildList[${index}].School`)
        .setAttribute('aria-activedescendant', option.id);

    activeIndex[index] = optionIndex;
}

function handleKeyDown(e, index) {
    const list = document.getElementById(`schoolList${index}`);
    if (list.hidden) return;

    const options = list.querySelectorAll('[role="option"]');

    switch (e.key) {
        case 'ArrowDown':
            e.preventDefault();
            setActive(index,
                activeIndex[index] < options.length - 1
                    ? activeIndex[index] + 1
                    : 0);
            break;

        case 'ArrowUp':
            e.preventDefault();
            setActive(index,
                activeIndex[index] > 0
                    ? activeIndex[index] - 1
                    : options.length - 1);
            break;

        case 'Enter':
            if (activeIndex[index] >= 0) {
                e.preventDefault();
                const school = resultCache[index][activeIndex[index]];
                selectSchool(school.name, school.id, school.la, school.postcode, index);
            }
            break;

        case 'Escape':
            closeList(index);
            break;
    }
}

// Set up event listeners for all school search inputs
let schoolSearch = document.getElementsByClassName("school-search");
for (let i = 0; i < schoolSearch.length; i++) {

    schoolSearch[i].addEventListener('input', function () {
        searchSchool(this.value, i);
    });

    schoolSearch[i].addEventListener('keydown', function (e) {
        handleKeyDown(e, i);
    });
}