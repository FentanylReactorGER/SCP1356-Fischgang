(function () {
  const sectionLinks = Array.from(document.querySelectorAll('.toc a[href^="#"], .nav a[href^="#"]'));
  const footerYear = document.getElementById("legalYear");

  if (footerYear) {
    const year = new Date().getFullYear();
    footerYear.textContent = `© ${year} Fanprojekt / SCP-inspirierte Lore-Seite`;
  }

  const sections = sectionLinks
    .map((link) => {
      const id = link.getAttribute("href");
      if (!id) return null;
      const section = document.querySelector(id);
      return section ? { link, section, id } : null;
    })
    .filter(Boolean);

  function setActive(id) {
    for (const entry of sections) {
      const active = entry.id === id;
      entry.link.classList.toggle("is-active", active);
    }
  }

  if ("IntersectionObserver" in window && sections.length > 0) {
    const observer = new IntersectionObserver(
      (entries) => {
        const visible = entries
          .filter((entry) => entry.isIntersecting)
          .sort((a, b) => b.intersectionRatio - a.intersectionRatio);

        if (visible.length > 0) {
          const currentId = `#${visible[0].target.id}`;
          setActive(currentId);
        }
      },
      {
        root: null,
        rootMargin: "-25% 0px -55% 0px",
        threshold: [0.15, 0.35, 0.6]
      }
    );

    for (const entry of sections) {
      observer.observe(entry.section);
    }
  }

  for (const link of sectionLinks) {
    link.addEventListener("click", () => {
      const href = link.getAttribute("href");
      if (href) {
        setActive(href);
      }
    });
  }
})();