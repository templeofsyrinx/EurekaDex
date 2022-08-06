echo "> Setting up ${{ github.repository_owner }}/DalamudPluginsD17"
gh repo clone "${{ github.repository_owner }}/DalamudPluginsD17" repo
cd repo
git remote add pr_repo "https://github.com/goatcorp/DalamudPluginsD17.git"
git fetch pr_repo
git fetch origin
echo "> Adding token to origin push url"
originUrl=$(git config --get remote.origin.url | cut -d '/' -f 3-)
originUrl="https://${{ secrets.PAT }}@${originUrl}"
git config remote.origin.url "${originUrl}"
branch="${{ env.PUBLIC_NAME }}"
if git show-ref --quiet "refs/heads/${branch}"; then
    echo "> Branch ${branch} already exists, reseting to master"
    git checkout "${branch}"
    git reset --hard "pr_repo/main"
else
    echo "> Creating new branch ${branch}"
    git reset --hard "pr_repo/main"
    git branch "${branch}"
    git checkout "${branch}"
    git push --set-upstream origin --force "${branch}"
fi
cd ..